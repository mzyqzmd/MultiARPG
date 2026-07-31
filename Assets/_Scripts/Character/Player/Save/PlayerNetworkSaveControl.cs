using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 玩家网络存档编排（NetworkBehaviour）：owner 权威同步身份的 NetworkVariable + 加入/上报/还原 RPC，
/// 全部收敛于本组件（挂在 Player 预制体同对象上，与 Player 各自独立 NetworkBehaviour 由 NGO 原生同步）。
/// 逻辑依赖 SaveService（Host 单一真源）与 PlayerSaveControl（本地数据组装），由 Player.Awake 经 Initialize 注入。
/// </summary>
public class PlayerNetworkSaveControl : NetworkBehaviour
{
    // owner 权威同步的玩家身份（供 Host 端副本快照存档，避免断联时 RPC 往返丢失）
    private readonly NetworkVariable<FixedString128Bytes> _playerId =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private GameSaveService _saveService;
    private PlayerSaveControl _saveControl;
    private PlayerStatsControl _statsControl;
    private NetworkService _networkService;

    /// <summary>依赖注入（MonoBehaviour 无法构造函数注入，由 Player.Awake 显式传入）</summary>
    public void Initialize(GameSaveService saveService_, PlayerSaveControl saveControl_, PlayerStatsControl statsControl_, NetworkService networkService_)
    {
        _saveService = saveService_;
        _saveControl = saveControl_;
        _statsControl = statsControl_;
        _networkService = networkService_;
    }

    #region 网络生命周期：owner 端写入身份并触发恢复/上报；断联时存档
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        _playerId.Value = LocalPlayerId.Value;
        if (IsHost)
            HostRestoreSelf();
        else
            JoinServerRpc(_saveControl.BuildPlayerSaveData(LocalPlayerId.Value));
    }

    public override void OnNetworkDespawn()
    {
        // 断联存档：Host 存世界+自身+收集所有客人；客人仅上报自身整包（best-effort）
        if (IsOwner)
        {
            if (IsHost)
                CoordinateSave();
            else
                ReportSaveToHost();
        }
        else if (IsServer)
        {
            // Bug 2：Host 侧 client 副本断联，直接从实时同步的副本快照存档（不依赖 RPC 往返）
            SnapshotClientSave();
        }
    }
    #endregion

    #region Host/客人 存档触发
    /// <summary>owner 端统一存档入口：替代 Player 内重复的 IsHost 分支。Host 协调全体存档，客人仅上报自身。</summary>
    public void RequestSave()
    {
        if (!IsOwner) return;
        if (IsHost) CoordinateSave();
        else ReportSaveToHost();
    }

    /// <summary>Host owner：从工作副本按 playerId 恢复自己；无记录则新建并加入 Players。</summary>
    private void HostRestoreSelf()
    {
        string id = LocalPlayerId.Value;
        var data = _saveService.GetPlayer(id);
        if (data != null)
        {
            _saveControl.RestoreFromPlayer(data);
            _statsControl.RestoreHealth(data.currentHealth);
            _saveControl.RestoreWorldTime(_saveService.GetWorld()?.secondsPlayed ?? 0f);
        }
        else
        {
            // 新游戏：尚无存档，按默认满血初始化（幂等）；
            // 先确保满血再建记录，使首帧存档 currentHealth 即满血而非 0
            _statsControl.ApplyDefaultHealth();
            _saveService.UpsertPlayer(_saveControl.BuildPlayerSaveData(id));
        }
    }

    /// <summary>Host：存自身 + 世界状态 + 遍历所有连入 client 副本快照收集，立即落盘（不依赖 RPC 往返）。</summary>
    public void CoordinateSave()
    {
        _saveService.UpsertPlayer(_saveControl.BuildPlayerSaveData(LocalPlayerId.Value));

        _networkService.ForEachClientPlayerObject(obj =>
        {
            if (obj.TryGetComponent(out PlayerNetworkSaveControl ctrl))
                _saveService.UpsertPlayer(ctrl.BuildReplicaSnapshot());
        });

        _saveService.SetWorld(_saveControl.BuildWorldSaveData());
        _saveService.SaveCurrentSlot();
    }

    /// <summary>owner：把自身整包存档上报给 Host（客人 checkpoint 用；断联 best-effort 由 Host 副本快照覆盖）。</summary>
    public void ReportSaveToHost()
    {
        if (!IsOwner) return;
        SubmitPlayerSaveServerRpc(_saveControl.BuildPlayerSaveData(LocalPlayerId.Value));
    }

    /// <summary>Host 端从 client 副本组装存档：复用本地组装取实时 transform，但 currentHealth 不读服务端客人副本
    /// （本地权威下该副本生命值已陈旧），改以客人主动上报的已有存档记录为准；
    /// 无记录时回退到副本当前值。再覆盖 characterName 自已有存档。</summary>
    public PlayerSaveData BuildReplicaSnapshot()
    {
        string id = _playerId.Value.ToString();
        var existing = _saveService.GetPlayer(id);
        var data = _saveControl.BuildPlayerSaveData(id);
        if (existing != null)
        {
            data.currentHealth = existing.currentHealth;
            data.characterName = existing.characterName;
        }
        return data;
    }

    /// <summary>Host 侧 client 副本断联：直接从副本快照写入单一真源并落盘。</summary>
    public void SnapshotClientSave()
    {
        _saveService.UpsertPlayer(BuildReplicaSnapshot());
        _saveService.SaveCurrentSlot();
    }
    #endregion

    #region RPC：加入/上报/还原
    // 客人加入：上报身份，命中已有记录则定向恢复，否则新建
    [ServerRpc]
    private void JoinServerRpc(PlayerSaveData incoming_, ServerRpcParams params_ = default)
    {
        var existing = _saveService.GetPlayer(incoming_.playerId);
        if (existing != null)
        {
            // server 端还原 Host 上该客人副本的生命值（本地，无 NV），确保 Host 计算其受伤基于正确初始值
            _statsControl.RestoreHealth(existing.currentHealth);
            var target = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { params_.Receive.SenderClientId } }
            };
            RestorePlayerClientRpc(existing, target);
        }
        else
            _saveService.UpsertPlayer(incoming_);
    }

    // 客人整包上报到 Host：合并进单一真源并落盘
    [ServerRpc]
    private void SubmitPlayerSaveServerRpc(PlayerSaveData data_)
    {
        _saveService.UpsertPlayer(data_);
        _saveService.SaveCurrentSlot();
    }

    // 定向下发：仅目标客人 owner 端还原自身坐标与生命（生命为纯本地，owner 端直接 RestoreHealth，无需回 server）
    [ClientRpc]
    private void RestorePlayerClientRpc(PlayerSaveData data_, ClientRpcParams _ = default)
    {
        if (!IsOwner) return;
        _saveControl.RestoreFromPlayer(data_);
        _statsControl.RestoreHealth(data_.currentHealth);
    }
    #endregion
}

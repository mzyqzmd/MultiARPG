using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 通用网络中转站（NetworkBehaviour，独立 NetworkObject 预制体）：收口跨端 RPC 广播。
/// 承载两类链路：① 战斗受击；② 玩家存档编排（去 NV 化、clientId 作 key 的缓存字典，替代原 per-player PlayerNetworkSaveControl）。
/// 联机启动（StartHost）时由 NetworkService 实例化并 Spawn，纯 Client 靠 NGO 同步自动获得实例。
/// 预制体需设 NetworkObject.DontDestroyWithOwner = true 以保证进 Scene_World（NGO SceneManager.LoadScene Single）不被销毁。
/// 服务经 Awake 注入为字段（项目规范：不在业务代码内联 Services.Get）。
/// </summary>
public class NetworkHub : NetworkBehaviour
{
    /// <summary>全局单例（OnNetworkSpawn 设，OnNetworkDespawn 清）。</summary>
    public static NetworkHub Instance { get; private set; }

    private VFXService _vfxService;
    private AudioService _audioService;
    private GameSaveService _saveService;

    /// <summary>客人/Host 上报的存档缓存：key=clientId，value=该端最近一次上报的 PlayerSaveData（断联兜底落盘用）。</summary>
    private readonly Dictionary<ulong, PlayerSaveData> _clientSaves = new();

    /// <summary>每个联机玩家的静默上报计时器：key=clientId，value=累计未上报时长（秒），到期由 Hub 集中驱动刷新缓存。</summary>
    private readonly Dictionary<ulong, float> _saveTimers = new();

    /// <summary>Host 自身玩家数据缓存（由 Host 的 Player 每帧刷入，Update 到期时搬入 _clientSaves 统一结构，不写盘）。</summary>
    private PlayerSaveData _hostCache;

    /// <summary>静默上报周期（秒）：到期仅刷新内存缓存，不触发落盘。</summary>
    private const float AutoSaveInterval = 5f;

    private void Awake()
    {
        _vfxService = Services.Get<VFXService>();
        _audioService = Services.Get<AudioService>();
        _saveService = Services.Get<GameSaveService>();
    }

    public override void OnNetworkSpawn()
    {
        if (Instance == null)
            Instance = this;
        // 断联兜底仅在 Host/Server 侧有意义：客人掉线时以其最后上报缓存落盘
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
            Instance = null;
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    /// <summary>客人/Host 掉线：用其最后上报缓存兜底落盘并清理，避免丢失存档。</summary>
    private void OnClientDisconnect(ulong clientId_)
    {
        if (_clientSaves.TryGetValue(clientId_, out var data))
        {
            _saveService.UpsertPlayer(data);
            _saveService.SaveCurrentSlot();
            _clientSaves.Remove(clientId_);
        }
        _saveTimers.Remove(clientId_);
    }

    #region 周期静默缓存刷新（仅内存更新，不落盘）
    /// <summary>Host 端集中驱动：遍历所有联机玩家累加计时，到期分派——客人经 ClientRpc 催报，Host 自身搬入本地缓存。</summary>
    private void Update()
    {
        if (!IsServer) return;
        var localId = NetworkManager.Singleton.LocalClientId;
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            _saveTimers.TryGetValue(id, out var t);
            t += Time.deltaTime;
            if (t >= AutoSaveInterval)
            {
                t = 0f;
                if (id == localId)
                {
                    RefreshHostCache();
                    Debug.Log($"[HUB] 周期到期 Host 自身缓存已刷新(未落盘) clientId={id}");
                }
                else
                {
                    RequestSilentReportClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { id } } });
                    Debug.Log($"[HUB] 周期到期 已向客人发送静默催报 clientId={id}");
                }
            }
            _saveTimers[id] = t;
        }
    }

    /// <summary>定向催客人上报：仅目标 owner 端响应，组装本地数据经 ServerRpc 静默刷新 Host 内存缓存。</summary>
    [ClientRpc]
    private void RequestSilentReportClientRpc(ClientRpcParams _ = default)
    {
        Debug.Log("[HUB] 客人收到静默催报，准备上报本地数据");
        ReportSaveSilently(_localDataProvider());
    }

    /// <summary>静默上报：仅更新 _clientSaves 内存字典，不合并真源、不写盘（落盘由断联/检查点/返回菜单触发）。</summary>
    [ServerRpc(RequireOwnership = false)]
    private void ReportSaveSilentServerRpc(PlayerSaveData data_, ServerRpcParams params_ = default)
    {
        _clientSaves[params_.Receive.SenderClientId] = data_;
        Debug.Log($"[HUB] 静默上报已更新内存缓存(未落盘) clientId={params_.Receive.SenderClientId}");
    }

    /// <summary>本地玩家数据提供方：由各端本地 Player(owner) 注册，ClientRpc 催报时取最新数据，避免 Hub 反向依赖 Player 类型。</summary>
    private System.Func<PlayerSaveData> _localDataProvider;

    /// <summary>本地 Player(owner) 注册自身数据提供方（每帧刷 _hostCache + 供 ClientRpc 取数）。</summary>
    public void RegisterLocalDataProvider(System.Func<PlayerSaveData> provider_) => _localDataProvider = provider_;

    /// <summary>Host 自身数据刷新入口：Player(Host) 每帧调用，仅更新本地缓存不落盘。</summary>
    public void UpdateHostCache(PlayerSaveData hostData_) => _hostCache = hostData_;

    /// <summary>到期把 Host 本地缓存搬入统一 _clientSaves 结构（不写盘）。</summary>
    private void RefreshHostCache() => _clientSaves[NetworkManager.Singleton.LocalClientId] = _hostCache;

    /// <summary>静默上报公开入口（供 ClientRpc 回调调用）：经 ServerRpc 写内存缓存。</summary>
    private void ReportSaveSilently(PlayerSaveData data_) => ReportSaveSilentServerRpc(data_);
    #endregion

    #region 战斗RPC（攻击者 owner 发起 → Host 定位目标 → 目标 owner 本地扣血 + 其他端播受击表现）
    /// <summary>攻击者本机命中后发起：把伤害发给 Host（RequireOwnership=false 允许非目标 owner 调用），由 Host 按 targetId 找到目标 Character 经 OnHitReceived 钩子统一驱动扣血 + 进受击态；受击表现（飙血 VFX + 受伤音效）仅广播给除攻击者外的其他客户端（攻击者本机已在命中瞬间本地播放，避免往返延迟与重复）。</summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestDamageServerRpc(ulong attackerId_, ulong targetId_, DamageInfo damage_)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetId_, out var targetObj)) return;
        if (!targetObj.TryGetComponent<Character>(out var target)) return;

        // 被击者即 Host 自身（owner 在 Host 进程）：本机直接驱动受击态 + 扣血 + 本地播受击表现
        if (target.IsOwner)
        {
            target.OnHitReceived(damage_, damage_.contactPoint);
            PlayHitReactionLocal(damage_.contactPoint);
            return;
        }

        // 被击者是远端 Client：让目标 owner 客户端本地进受击态（ClientNetworkAnimator 为 owner 权威，
        // 只有 owner 端驱动的 HitState 才会同步给其他人；Host 副本本地调不会反向同步给 Client）
        NotifyHitClientRpc(targetId_, damage_);

        // 旁观端（排除攻击者与被击者 owner）统一播受击表现 VFX/音效
        var spectators = new System.Collections.Generic.List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        spectators.Remove(attackerId_);
        spectators.Remove(target.OwnerClientId);
        if (spectators.Count == 0) return;
        var param = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = spectators.ToArray() }
        };
        PlayHitReactClientRpc(damage_.contactPoint, param);
    }

    /// <summary>本地受击表现（飙血 VFX + 受伤音效 + 血溅音效）：各端基于同一 contactPoint 世界坐标本地播，
    /// 视觉一致且无需跨端 RPC 额外同步（VFXService/AudioService 均为纯本地）。被击者自身、Host 自击分支、旁观端共用。</summary>
    private void PlayHitReactionLocal(Vector3 contactPoint_)
    {
        _vfxService.PlayVFX("FX_BloodSplatter", contactPoint_);
        _audioService.PlayAudio("Player", "BloodSplatter", contactPoint_);
        _audioService.PlayAudio("Player", "Hit", contactPoint_);
    }

    /// <summary>定向被击者 owner 客户端：让其本地进受击态 + 扣血 + 本地播受击表现（受击动画靠 ClientNetworkAnimator owner 权威同步出去）。</summary>
    [ClientRpc]
    private void NotifyHitClientRpc(ulong targetId_, DamageInfo damage_)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetId_, out var targetObj)) return;
        if (!targetObj.TryGetComponent<Character>(out var target)) return;
        // 仅被击者自身的 owner 端响应（ClientRpc 已定向，但守卫一次以防误投）
        if (!target.IsOwner) return;
        target.OnHitReceived(damage_, damage_.contactPoint);
        PlayHitReactionLocal(damage_.contactPoint);
    }

    /// <summary>受击表现广播：在 contactPoint 世界坐标播飙血 VFX + 受伤音效。发往指定客户端（默认全端，由调用方排除攻击者与被击者 owner）。</summary>
    [ClientRpc]
    private void PlayHitReactClientRpc(Vector3 contactPoint_, ClientRpcParams param_ = default)
    {
        _vfxService.PlayVFX("FX_BloodSplatter", contactPoint_);
        _audioService.PlayAudio("Player", "BloodSplatter", contactPoint_);
        _audioService.PlayAudio("Player", "Hit", contactPoint_);
    }
    #endregion

    #region 玩家存档编排（替代原 per-player PlayerNetworkSaveControl，去 NV 化 + 集中字典）
    /// <summary>客人/Host 本地加入时调用：打包自身 PlayerSaveData 上报 Host。命中已有记录则定向还原坐标与血量，否则新建。</summary>
    public void RequestJoin(PlayerSaveData data_) => JoinServerRpc(data_);

    /// <summary>客人/Host 本地上报存档：打包自身 PlayerSaveData 上报 Host 合并落盘（RequireOwnership=false 允许非 owner 调用）。</summary>
    public void RequestSave(PlayerSaveData data_) => SubmitPlayerSaveServerRpc(data_);

    /// <summary>Host 本地协调全体存档：合并自身 + 各客人缓存 + 世界状态后立即落盘（替代原 CoordinateSave 遍历 per-player 副本）。</summary>
    public void CoordinateSave(PlayerSaveData hostData_, WorldSaveData worldData_)
    {
        if (!IsServer) return;
        _saveService.UpsertPlayer(hostData_);
        foreach (var kvp in _clientSaves)
            _saveService.UpsertPlayer(kvp.Value);
        _saveService.SetWorld(worldData_);
        _saveService.SaveCurrentSlot();
    }

    /// <summary>客人加入：上报身份，命中已有记录则定向恢复，否则新建（缓存以备断联兜底）。</summary>
    [ServerRpc(RequireOwnership = false)]
    private void JoinServerRpc(PlayerSaveData incoming_, ServerRpcParams params_ = default)
    {
        ulong sender = params_.Receive.SenderClientId;
        _clientSaves[sender] = incoming_;
        var existing = _saveService.GetPlayer(incoming_.playerId);
        if (existing != null)
        {
            var target = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } }
            };
            RestorePlayerClientRpc(existing, target);
        }
        else
        {
            _saveService.UpsertPlayer(incoming_);
        }
    }

    /// <summary>客人/Host 整包上报到 Host：覆盖缓存并合并进单一真源落盘。</summary>
    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlayerSaveServerRpc(PlayerSaveData data_, ServerRpcParams params_ = default)
    {
        _clientSaves[params_.Receive.SenderClientId] = data_;
        _saveService.UpsertPlayer(data_);
        _saveService.SaveCurrentSlot();
        Debug.Log($"[HUB] 真正落盘 SaveCurrentSlot (检查点/返回菜单) clientId={params_.Receive.SenderClientId}");
    }

    /// <summary>定向下发：仅目标客人 owner 端还原自身坐标与生命（生命为纯本地，owner 端直接 RestoreHealth，无需回 server）。</summary>
    [ClientRpc]
    private void RestorePlayerClientRpc(PlayerSaveData data_, ClientRpcParams _ = default)
    {
        // 由 Player 订阅/转发执行本地还原；Hub 不直接持有玩家 transform
        OnPlayerRestore?.Invoke(data_);
    }

    /// <summary>玩家存档还原事件：Player 本地订阅，收到后 RestoreFromPlayer + RestoreHealth。</summary>
    public event System.Action<PlayerSaveData> OnPlayerRestore;
    #endregion
}

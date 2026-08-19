using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// AI NPC 全局调度服务（IService 普通类）：收集所有 AINPC 注册表，按全部在线玩家距离低频轮询激活/失活以省性能。
/// 仅 Host/Server 端有意义（行为树仅 Host 跑），客户端服务方法为空操作；激活态经 NetworkAnimator 广播至各客户端。
/// </summary>
public class AINpcService : IService
{
    private readonly List<AINPC> _npcs = new();
    private float _timer;
    private const float PollInterval = 0.2f;

    #region NPC主动注册/反注册至服务
    /// <summary>注册 NPC：由 AINPC 基类在 OnNetworkSpawn 自报存在。</summary>
    public void RegisterNpc(AINPC npc_)
    {
        if (npc_ == null || _npcs.Contains(npc_)) return;
        _npcs.Add(npc_);
    }

    /// <summary>反注册 NPC：由 AINPC 基类在 OnNetworkDespawn 移除。</summary>
    public void UnregisterNpc(AINPC npc_)
    {
        if (npc_ == null) return;
        _npcs.Remove(npc_);
    }
    #endregion

    #region 低频按全部在线玩家距离切换 NPC 激活态
    public void Tick()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        _timer += Time.deltaTime;
        if (_timer < PollInterval) return;
        _timer = 0f;

        var players = PlayerTransforms;
        foreach (var npc in _npcs)
        {
            if (npc == null) continue;
            EvaluateActivation(npc, players);
        }
    }

    /// <summary>全部在线玩家的 Transform 列表（仅 Host 有效，自动随上下线更新）；行为树节点经此取玩家，避免各自依赖 NetworkManager。</summary>
    public List<Transform> PlayerTransforms
    {
        get
        {
            if (!NetworkManager.Singleton.IsServer) return new List<Transform>();
            var result = new List<Transform>();
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client == null || client.PlayerObject == null) continue;
                var transform_ = client.PlayerObject.transform;
                if (transform_ != null) result.Add(transform_);
            }
            return result;
        }
    }

    /// <summary>任一玩家在 ActivationRadius 内即激活；全超出则失活。目标选取交由行为树，此处不写 TargetPlayer。</summary>
    private void EvaluateActivation(AINPC npc_, List<Transform> players_)
    {
        float radiusSqr = npc_.ActivationRadius * npc_.ActivationRadius;
        foreach (var player in players_)
        {
            if (player == null) continue;
            float sqr = (player.position - npc_.transform.position).sqrMagnitude;
            if (sqr <= radiusSqr)
            {
                npc_.SetActive(true);
                return;
            }
        }

        npc_.SetActive(false);
    }
    #endregion
}

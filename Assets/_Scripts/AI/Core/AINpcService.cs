using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// AI NPC 全局调度服务（IService 普通类）：场景加载后收集所有 AINPC，按本地玩家距离低频轮询激活/失活以省性能。
/// 仅 Host/Server 端有意义（行为树仅 Host 跑），客户端服务方法为空操作。
/// </summary>
public class AINpcService : IService
{
    private readonly List<AINPC> _npcs = new();
    private Transform _localPlayer;
    private float _timer;
    private bool _initialized;
    private const float PollInterval = 0.2f;

    /// <summary>场景加载后由激活方（如 SceneControlService 或 GameRoot）调用，收集所有场景内置 AI NPC。</summary>
    public void RegisterAll()
    {
        _npcs.Clear();
        _npcs.AddRange(Object.FindObjectsByType<AINPC>(FindObjectsSortMode.None));
    }

    /// <summary>每帧由宿主 MonoBehaviour（GameRoot）驱动：低频按玩家距离切换 NPC 激活态。</summary>
    public void Tick()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (!_initialized)
        {
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient != null && localClient.PlayerObject != null)
            {
                _localPlayer = localClient.PlayerObject.transform;
                RegisterAll();
                _initialized = true;
            }
            else return;
        }

        _timer += Time.deltaTime;
        if (_timer < PollInterval) return;
        _timer = 0f;

        foreach (var npc in _npcs)
        {
            if (npc == null) continue;
            bool inRange = Vector3.Distance(_localPlayer.position, npc.transform.position) <= npc.ActivationRadius;
            npc.SetActive(inRange);
        }
    }
}

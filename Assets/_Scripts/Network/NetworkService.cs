using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 网络服务（IService）：项目唯一网络入口。
/// 主菜单两条明确路径：读取存档 → StartHost（由 Host 加载世界）；加入房间 → JoinRoom（仅 Client）。
/// 纯 NGO，无额外发现逻辑。
/// </summary>
public class NetworkService : IService
{
    /// <summary>加入房间失败事件（超时仍未连上 Host 时触发），UI 订阅后弹 PopText。</summary>
    public event Action OnJoinFailed;

    #region 显式升 Host（读取存档 / 新建存档后由主菜单触发）
    /// <summary>作为 Host 启动（已选/新建存档后调用，随后由 SceneControlService 加载世界）。</summary>
    public void StartHost()
    {
        var mgr = NetworkManager.Singleton;
        if (mgr == null || mgr.IsHost || mgr.IsServer) return;
        mgr.StartHost();
    }
    #endregion

    #region 加入房间（仅 Client，超时未连上 Host 则关闭并触发 OnJoinFailed，不升 Host）
    /// <summary>仅作为 Client 尝试连接；超时仍未连上 Host 则关闭 client 并触发 OnJoinFailed。</summary>
    public void JoinRoom(float timeoutSeconds_ = 1f)
    {
        var mgr = NetworkManager.Singleton;
        if (mgr == null) return;
        mgr.StartClient();
        mgr.StartCoroutine(TryJoinRoom(timeoutSeconds_));
    }

    // 仅 Client 尝试连接；超时仍未连上 Host，则关闭 client 并触发 OnJoinFailed
    private IEnumerator TryJoinRoom(float timeout_)
    {
        var mgr = NetworkManager.Singleton;
        float timer = 0f;
        while (timer < timeout_ && !mgr.IsConnectedClient)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        if (mgr.IsConnectedClient) yield break;
        if (mgr.IsClient) mgr.Shutdown();
        OnJoinFailed?.Invoke();
    }
    #endregion
}

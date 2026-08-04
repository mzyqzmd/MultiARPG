using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;

/// <summary>
/// 网络服务（IService）：项目唯一网络入口。
/// 主菜单两条明确路径：读取存档 → StartHost（由 Host 加载世界）；加入房间 → JoinRoom（仅 Client）。
/// 场景加载与远端玩家遍历等 NGO 触点统一收归此处，避免业务层（SceneControlService / 存档组件）直连 NetworkManager。
/// 纯 NGO，无额外发现逻辑。
/// </summary>
public class NetworkService : IService
{
    /// <summary>加入房间失败事件（超时仍未连上 Host 时触发），UI 订阅后弹 PopText。</summary>
    public event Action OnJoinFailed;

    /// <summary>本机是否为 Host/Server（场景加载等能力仅 Host 可发起）。</summary>
    public bool IsHost => NetworkManager.Singleton.IsHost;

    private readonly NetworkHub _hubPrefab;
    private readonly UIService _uiService;

    public NetworkService(NetworkHub hubPrefab_, UIService uiService_)
    {
        _uiService = uiService_;
        _hubPrefab = hubPrefab_;
    }

    #region 显式升 Host（读取存档 / 新建存档后由主菜单触发）
    /// <summary>作为 Host 启动：启动 Host 后立即 Spawn 通用网络中转站 NetworkHub，使其随联机常驻（纯 Client 经 NGO 同步自动获得，无需自行 Spawn）。</summary>
    public void StartHost()
    {
        var mgr = NetworkManager.Singleton;
        if (mgr.IsHost || mgr.IsServer) return;
        mgr.StartHost();
        // 联机启动即 Spawn Hub（跨场景靠预制体 NetworkObject.DontDestroyWithOwner=true 存活，避免进 World 场景被销毁）
        if (_hubPrefab != null)
            UnityEngine.Object.Instantiate(_hubPrefab).GetComponent<NetworkObject>().Spawn();
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

    #region 场景加载（仅 Host 可发起；统一 NGO SceneManager，禁止原生 SceneManager 以免销毁 Player 网络对象）
    /// <summary>由 Host 驱动，同步所有客户端加载场景（Single 模式）。非 Host 调用静默无效。</summary>
    public void LoadScene(string sceneName_)
    {
        var mgr = NetworkManager.Singleton;
        if (!mgr.IsHost) return;
        mgr.SceneManager.LoadScene(sceneName_, LoadSceneMode.Single);
    }
    #endregion

    #region 断开并返回主菜单（仅 Client 返回路径：断开连接 + 原生切场景）
    /// <summary>Client 返回主菜单时调用：先经 ReturningToMainMenuEvent 广播完成本地存档上报，
    /// 再此处 Shutdown 断开，随后原生切回主菜单。已断开后网络对象随断连销毁，
    /// 原生 LoadScene 属"禁原生 LoadScene"铁律的安全例外（无残留网络对象可丢）。</summary>
    public void DisconnectAndReturnToMainMenu()
    {
        // 已 Shutdown 断开，网络对象随断连销毁，原生切场景为"禁原生 LoadScene"铁律的安全例外。
        // Fade 渐变由 SceneControlService.ReturnToMainMenu 客人分支统一编排（设 _transitioning + FadeToBlack），
        // 切完经 OnActiveSceneChanged 自动 FadeFromBlack，避免黑幕卡死。
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(SceneControlService.SceneMainMenu);
    }
    #endregion

    #region 远端玩家遍历（供 Host 侧副本快照存档，跳过 Host 自身）
    /// <summary>遍历除 Host 自身外的所有连入 client 的 PlayerObject，回调交由业务侧取组件（避免本服务反向依赖角色层类型）。</summary>
    public void ForEachClientPlayerObject(Action<NetworkObject> action_)
    {
        var mgr = NetworkManager.Singleton;
        if (action_ == null) return;
        foreach (var client in mgr.ConnectedClientsList)
        {
            if (client.ClientId == mgr.LocalClientId) continue;
            if (client.PlayerObject != null) action_(client.PlayerObject);
        }
    }
    #endregion
}

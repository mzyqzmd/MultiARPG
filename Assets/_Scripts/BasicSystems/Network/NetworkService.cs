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

    /// <summary>被动断线事件（Client 端）：Host 退出/掉线导致连接断开时触发（主动退出与加入超时不触发），场景服务订阅后驱动返回主菜单。</summary>
    public event Action OnDisconnectedFromHost;

    /// <summary>本机是否为 Host/Server（场景加载等能力仅 Host 可发起）。</summary>
    public bool IsHost => NetworkManager.Singleton.IsHost;

    private readonly NetworkHub _hubPrefab;
    private readonly UIService _uiService;

    /// <summary>主动断开一次性标志：主动退出/加入超时的 Shutdown 前置位，OnClientStopped 回调消费即复位，防误判为被动断线。</summary>
    private bool _intentionalShutdown;

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
        BindClientStopped(mgr);
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
        BindClientStopped(mgr);
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
        if (mgr.IsClient)
        {
            _intentionalShutdown = true;    // 加入失败主动关闭：不触发被动断线事件
            mgr.Shutdown();
        }
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

    #region 断开并返回主菜单（NGO 有序 Shutdown：先 Shutdown + 订阅 OnClientStopped/OnServerStopped 真完成后再原生切场景）
    /// <summary>发起返回主菜单：先 Shutdown 断开，待 NGO 完全清场（OnClientStopped/OnServerStopped 触发）后原生切回主菜单。
    /// 已断开后网络对象随断连销毁，原生 LoadScene 属"禁原生 LoadScene"铁律的安全例外（无残留网络对象可丢）。
    /// Fade 渐变由 SceneControlService.ReturnToMainMenu 统一编排（设 _transitioning + FadeToBlack），切完经 OnActiveSceneChanged 自动 FadeFromBlack。</summary>
    public void BeginReturnToMainMenu()
    {
        var mgr = NetworkManager.Singleton;
        // 已断开（被动断线后 NGO 已自动 Shutdown / 主动退出途中对端先退出）：网络对象已随断连销毁，直接原生切回主菜单
        if (mgr == null || (!mgr.IsServer && !mgr.IsClient))
        {
            SceneManager.LoadScene(SceneControlService.SceneMainMenu);
            return;
        }
        _intentionalShutdown = true;    // 主动断开标志：兜住本机 OnClientStopped，防误触发被动断线事件
        mgr.Shutdown();
        mgr.StartCoroutine(WaitShutdownThenLoadScene(SceneControlService.SceneMainMenu));
    }

    /// <summary>等待 NGO 真正停止后再原生切场景，避免同帧 Shutdown + LoadScene 导致 NGO 状态脏。</summary>
    private IEnumerator WaitShutdownThenLoadScene(string sceneName_)
    {
        var mgr = NetworkManager.Singleton;
        if (mgr == null) { SceneManager.LoadScene(sceneName_); yield break; }
        bool done = false;
        System.Action<bool> onStop = null;
        onStop = _ => { mgr.OnClientStopped -= onStop; mgr.OnServerStopped -= onStop; done = true; };
        mgr.OnClientStopped += onStop;
        mgr.OnServerStopped += onStop;
        float t = 0f;
        while (!done && t < 5f) { t += Time.deltaTime; yield return null; }
        if (!done) Debug.LogError("[NetworkService] OnServerStopped/OnClientStopped 未在5s内触发，强制切场景");
        SceneManager.LoadScene(sceneName_);
    }

    // 订阅 NGO client 停止回调（先减后加幂等，StartHost/JoinRoom 可重复调用；NetworkManager 常驻无泄漏风险）
    private void BindClientStopped(NetworkManager mgr_)
    {
        mgr_.OnClientStopped -= HandleClientStopped;
        mgr_.OnClientStopped += HandleClientStopped;
    }

    // OnClientStopped 回调：主动断开标志为真则消费复位并跳过（主动退出/加入超时），否则视为被动断线（Host 退出/掉线）广播事件
    private void HandleClientStopped(bool _)
    {
        if (_intentionalShutdown)
        {
            _intentionalShutdown = false;
            return;
        }
        OnDisconnectedFromHost?.Invoke();
    }
    #endregion

    #region 旧路径（已弃用，保留兼容外层残留调用）
    [Obsolete("改用 BeginReturnToMainMenu（按 NGO 有序 Shutdown）")]
    public void DisconnectAndReturnToMainMenu()
    {
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

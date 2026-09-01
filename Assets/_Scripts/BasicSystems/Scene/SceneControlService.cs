using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景控制服务（IService）：负责世界/关卡场景切换。
/// 多人场景下必须用 NGO 的 SceneManager 而非原生 SceneManager——
/// 否则 Player 等网络对象会被场景卸载销毁，且各客户端场景不同步。
/// NGO 场景管理会在切场景时自动保留 Player 网络对象，无需手动 DontDestroyOnLoad。
/// 场景加载能力统一收归 NetworkService（IsHost 守卫 + NGO SceneManager），本服务不再直连 NetworkManager。
/// </summary>
public class SceneControlService : IService
{
    public const string SceneMainMenu = "Scene_MainMenu";
    public const string SceneWorld = "Scene_World";

    private readonly InputService _inputService;
    private readonly UIService _uiService;
    private readonly NetworkService _networkService;
    private bool _transitioning;
    public Scene CurrentActiveScene { get; private set; }

    public SceneControlService(InputService inputService_, UIService uiService_, NetworkService networkService_)
    {
        _inputService = inputService_;
        _uiService = uiService_;
        _networkService = networkService_;
        CurrentActiveScene = SceneManager.GetActiveScene();
        // 订阅 Unity 原生场景切换：Host 与 late-join Client 都会触发，
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    #region 载入新游戏（世界场景）
    /// <summary>
    /// 由 Host/Server 驱动，同步所有客户端加载世界场景。
    /// 仅 Host 可发起场景切换，纯 Client 端调用无效（需由 Host 触发或经 ServerRpc 请求）。
    /// 切换前先渐黑，黑幕完成后才发起加载（经 NetworkService 收归 NGO SceneManager）。
    /// </summary>
    public void LoadWorldScene(string sceneName = SceneWorld)
    {
        if (!_networkService.IsHost) return;
        _transitioning = true;
        _uiService.FadeToBlack(() => _networkService.LoadScene(sceneName));
    }
    #endregion

    #region 返回主菜单（存档由 Player 自管，场景服务只发信号 + 切场景）
    /// <summary>先触发 OnReturningToMainMenu（此刻 Player 仍在世界场景、坐标正确），
    /// 随后 NGO 切回主菜单（保留网络对象）。本方法不认识 Player 字段、不组装存档数据。</summary>
    public void ReturnToMainMenu()
    {
        EventBus<ReturningToMainMenuEvent>.InvokeEvent(new ReturningToMainMenuEvent());
        // Host/Client 统一走断开式返回：Shutdown 销毁 Player 网络对象，重进世界为干净连接（与 Client 行为一致）。
        _transitioning = true;
        _uiService.FadeToBlack(() => _networkService.DisconnectAndReturnToMainMenu());
    }

    #endregion

    #region 输入模式随世界场景加载切换（Host 与 late-join Client 统一走此路径）
    // 本机完成场景切换时切换输入模式：进世界→玩家输入，回主菜单→UI 输入。
    // 若本次切换由本服务发起（_transitioning），黑幕完成后淡入。
    private void OnActiveSceneChanged(Scene previous_, Scene current_)
    {
        CurrentActiveScene = current_;
        switch (current_.name)
        {
            case SceneMainMenu:
                _inputService.EnableUIMode();
                break;
            default:
                _inputService.EnablePlayerMode();
                break;
        }
        if (_transitioning)
        {
            _transitioning = false;
            _uiService.FadeFromBlack();
        }
    }
    #endregion

    #region 应用焦点变化（由 GameRoot 转发 Unity 消息）
    /// <summary>应用焦点变化：失焦禁用全部输入，回焦按当前场景恢复对应输入模式。</summary>
    public void OnApplicationFocusChanged(bool focus_)
    {
        if (focus_)
        {
            if (CurrentActiveScene.name == SceneMainMenu)
                _inputService.EnableUIMode();
            else
                _inputService.EnablePlayerMode();
        }
        else _inputService.DisableAllMode();
    }
    #endregion

}

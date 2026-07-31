using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 主菜单面板（MVP 的 View + 菜单控制器）：先点「按下以继续」展开四个并列按钮，
/// 分别负责 读取存档 / 自动加入房间 / 设置 / 退出游戏。
/// </summary>
public class UI_MainMenuPanel : UI_BaseView
{
    [SerializeField] private Button _pressToContinueButton;

    [SerializeField] private GameObject _buttonGroup;
    [SerializeField] private Button _getSaveButton;
    [SerializeField] private UI_SaveSlotPanel _saveSlotPanel;
    [SerializeField] private Button _autoJoinButton;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Button _quitButton;

    private InputService _inputService;
    private NetworkService _networkService;
    private UIService _uiService;
    private GameSaveService _saveService;
    private SceneControlService _sceneControlService;
    private SettingsSaveService _settingsService;

    private void OnEnable()
    {
        _inputService = Services.Get<InputService>();
        _networkService = Services.Get<NetworkService>();
        _uiService = Services.Get<UIService>();
        _saveService = Services.Get<GameSaveService>();
        _sceneControlService = Services.Get<SceneControlService>();
        _settingsService = Services.Get<SettingsSaveService>();
        _inputService.OnSelectedButtonClicked += ConfirmSelectedButton;
        _networkService.OnJoinFailed += OnJoinFailed;

        _pressToContinueButton.onClick.AddListener(OnPressToContinue);
        _getSaveButton.onClick.AddListener(OnLoadSaveClicked);
        _autoJoinButton.onClick.AddListener(OnAutoJoinClicked);
        _settingButton.onClick.AddListener(OnSettingClicked);
        _quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnDisable()
    {
        _inputService.OnSelectedButtonClicked -= ConfirmSelectedButton;
        _networkService.OnJoinFailed -= OnJoinFailed;

        _pressToContinueButton.onClick.RemoveListener(OnPressToContinue);
        _getSaveButton.onClick.RemoveListener(OnLoadSaveClicked);
        _autoJoinButton.onClick.RemoveListener(OnAutoJoinClicked);
        _settingButton.onClick.RemoveListener(OnSettingClicked);
        _quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    // 确认键按下：触发 EventSystem 当前选中的按钮（Button 通过 ISubmitHandler 原生响应）
    private void ConfirmSelectedButton() =>
        ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,
            new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

    #region 按下以继续：隐藏自身并展开四个并列按钮
    private void OnPressToContinue()
    {
        _pressToContinueButton.gameObject.SetActive(false);
        _buttonGroup.SetActive(true);
        _getSaveButton.Select();
    }
    #endregion

    #region 读取存档：打开存档槽面板（隐藏主菜单按钮组，关闭时还原）
    private void OnLoadSaveClicked()
    {
        _buttonGroup.SetActive(false);
        _saveSlotPanel.Show();
        _saveSlotPanel.Initialize(_uiService, _saveService, _networkService, _sceneControlService,
            () => { _buttonGroup.SetActive(true); _getSaveButton.Select(); });
    }
    #endregion

    #region 自动加入房间：仅 Client 尝试连接，失败由 OnJoinFailed 弹提示
    private void OnAutoJoinClicked() => _networkService.JoinRoom();

    // 加入房间失败：弹顶部提示文本
    private void OnJoinFailed() =>
        _uiService.SpawnUiView<UI_PopText>("PopText", EUILayerCanvas.Overlay)
            .Initialize("未检测到已有房间,加入房间失败", Color.red);
    #endregion

    // 设置面板：隐藏主菜单按钮组（避免手柄导航按方向跳到面板后的按钮，详见导航不查遮挡），
    // 打开后焦点由面板内部 SelectFirst 接管；关闭时还原按钮组并重新选中设置按钮。
    private void OnSettingClicked()
    {
        _buttonGroup.SetActive(false);
        _uiService.SpawnUiView<UI_SettingPanel>("SettingPanel", EUILayerCanvas.Overlay)
            .Initialize(_settingsService, () =>
            {
                _buttonGroup.SetActive(true);
                _settingButton.Select();
            });
    }

    #region 退出游戏
    private void OnQuitClicked()
    {
        _inputService.DisableAllMode();
        Debug.Log("禁用所有输入");
        _uiService.FadeToBlack(() =>
        {
            Debug.Log("退出游戏");
            Application.Quit();
        });
    }
    #endregion
}

using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ESC 暂停面板（View）：世界场景中按 ESC / 手柄 Start 弹出，暂停玩家玩法输入（不冻结世界）。
/// 四按钮：继续（关闭并恢复输入）、设置（打开设置面板）、操作说明（打开操作说明面板）、保存并退出（按 Host/Client 分流返回主菜单）。
/// </summary>
public class UI_EscPanel : UI_BaseView
{
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Button _OperationTipButton;
    [SerializeField] private Button _saveAndQuitButton;

    private UIService _uiService;
    private InputService _inputService;
    private SceneControlService _sceneControlService;
    private SettingsSaveService _settingsService;

    #region 初始化（注入依赖 + 绑定按钮 + 暂停玩法输入）
    public void Initialize(UIService uiService_)
    {
        _uiService = uiService_;
        _inputService = Services.Get<InputService>();
        _sceneControlService = Services.Get<SceneControlService>();
        _settingsService = Services.Get<SettingsSaveService>();

        _continueButton.onClick.AddListener(CloseSelf);
        _settingButton.onClick.AddListener(OpenSetting);
        _OperationTipButton.onClick.AddListener(OpenOperationTip);
        _saveAndQuitButton.onClick.AddListener(SaveAndQuit);

        _inputService.DisableGameplayKeepEsc();
        _continueButton.Select();
    }
    #endregion

    #region 按钮回调
    // 继续：恢复玩法输入并关闭面板
    private void CloseSelf()
    {
        _inputService.EnablePlayerMode();
        _uiService.CloseEscPanel();
    }

    // 设置：打开现有设置面板，返回时销毁该面板自身
    private void OpenSetting()
    {
        var panel = _uiService.SpawnUiView<UI_SettingPanel>("SettingPanel", EUILayerCanvas.Overlay);
        SetInteractable(false);
        panel.Initialize(_settingsService, () =>
        {
            SetInteractable(true);
            _settingButton.Select();
            panel.Close();
        });
    }

    // 操作说明：打开极简操作说明面板（自带关闭按钮）
    private void OpenOperationTip()
    {
        var panel = _uiService.SpawnUiView<UI_OperationTipPanel>("OperationTipPanel", EUILayerCanvas.Overlay);
        SetInteractable(false);
        panel.OnClosed += () =>
        {
            SetInteractable(true);
            _OperationTipButton.Select();
        };
        panel.Initialize();
    }

    // 保存并退出：本机经 ReturnToMainMenu 断开返回主菜单；对端 Client 由 NetworkService 被动断线检测自动返回并弹断线提示
    private void SaveAndQuit()
    {
        _uiService.CloseEscPanel();
        _sceneControlService.ReturnToMainMenu();
    }
    #endregion

    #region 子面板共存时的手柄导航隔离（禁用自身按钮，避免穿透到背景 ESC 按钮）
    private void SetInteractable(bool interactable_)
    {
        _continueButton.interactable = interactable_;
        _settingButton.interactable = interactable_;
        _OperationTipButton.interactable = interactable_;
        _saveAndQuitButton.interactable = interactable_;
    }
    #endregion
}

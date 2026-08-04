using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置面板（MVP 的 View）：纯展示两个音量滑块（主音量 / 音效）+ 返回按钮，
/// 不含任何逻辑——滑块拖动/返回点击经事件抛给 Presenter，初值由 Presenter 经 SetXxx 回填。
/// </summary>
public class UI_SettingPanel : UI_BaseView
{
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Button _returnButton;


    public event Action<float> OnMusicVolumeChanged;
    public event Action<float> OnSfxVolumeChanged;
    public event Action OnCloseClicked;

    /// <summary>由组合根/入口注入设置服务，构造 Presenter 接管逻辑。</summary>
    public void Initialize(SettingsSaveService settingsService_, Action onClose_)
    {
        new SettingPanelPresenter(settingsService_, this);
        OnCloseClicked -= onClose_;
        OnCloseClicked += onClose_;
    }

    private void OnEnable()
    {
        _musicVolumeSlider.onValueChanged.AddListener(RaiseMusicChanged);
        _sfxVolumeSlider.onValueChanged.AddListener(RaiseSfxChanged);
        _returnButton.onClick.AddListener(RaiseClose);
    }

    private void OnDisable()
    {
        _musicVolumeSlider.onValueChanged.RemoveListener(RaiseMusicChanged);
        _sfxVolumeSlider.onValueChanged.RemoveListener(RaiseSfxChanged);
        _returnButton.onClick.RemoveListener(RaiseClose);
    }

    // 回填初值（不触发 onValueChanged，避免回环）
    public void SetMusicVolume(float value_) => _musicVolumeSlider.SetValueWithoutNotify(value_);
    public void SetSfxVolume(float value_) => _sfxVolumeSlider.SetValueWithoutNotify(value_);

    // 选中音乐音量滑块（供打开面板时聚焦，便于手柄导航）
    public void SelectFirst() => _musicVolumeSlider.Select();

    // 关闭并销毁自身（Presenter 落盘后调用）
    public void Close() => Destroy(gameObject);

    private void RaiseMusicChanged(float value_) => OnMusicVolumeChanged?.Invoke(value_);
    private void RaiseSfxChanged(float value_) => OnSfxVolumeChanged?.Invoke(value_);
    private void RaiseClose() => OnCloseClicked?.Invoke();
}

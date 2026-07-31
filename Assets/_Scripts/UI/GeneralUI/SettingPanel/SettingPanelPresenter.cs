/// <summary>
/// 设置面板 Presenter（MVP 的 Presenter）：构造注入 SettingsService + View，持有全部逻辑。
/// 初始化回填滑块；拖动时改 live 设置并 NotifyChanged 做实时预览（不写盘）；返回时 Save 落盘并关闭面板。
/// </summary>
public class SettingPanelPresenter
{
    private readonly SettingsSaveService _settingsService;
    private readonly UI_SettingPanel _view;

    public SettingPanelPresenter(SettingsSaveService settingsService_, UI_SettingPanel view_)
    {
        _settingsService = settingsService_;
        _view = view_;

        // 回填当前值
        _view.SetMusicVolume(_settingsService.SettingsData.musicVolume);
        _view.SetSfxVolume(_settingsService.SettingsData.sfxVolume);
        _view.SelectFirst();

        // 订阅 View 事件
        _view.OnMusicVolumeChanged += OnMusicVolumeChanged;
        _view.OnSfxVolumeChanged += OnSfxVolumeChanged;
        _view.OnCloseClicked += OnReturnClicked;
    }

    // 拖动主音量：改 live 设置 + 实时预览（AudioService 订阅 OnChanged 立即 ApplyVolumes）
    private void OnMusicVolumeChanged(float value_)
    {
        _settingsService.SettingsData.musicVolume = value_;
        _settingsService.NotifyChanged();
    }

    // 拖动音效音量：同上
    private void OnSfxVolumeChanged(float value_)
    {
        _settingsService.SettingsData.sfxVolume = value_;
        _settingsService.NotifyChanged();
    }

    // 返回：落盘并关闭面板
    private void OnReturnClicked()
    {
        _settingsService.Save();
        _view.OnMusicVolumeChanged -= OnMusicVolumeChanged;
        _view.OnSfxVolumeChanged -= OnSfxVolumeChanged;
        _view.OnCloseClicked -= OnReturnClicked;
        _view.Close();
    }
}

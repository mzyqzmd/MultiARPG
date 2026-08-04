using System;

/// <summary>
/// 全局设置服务（IService）：通用全局配置入口，持久化到独立 .setting 文件（persistentDataPath/Settings/），
/// 不进 GameSaveData、不联网。复用泛型化的 SaveFileDataWriter 做 JSON IO，不另建持久化实现。
/// 修改期实时预览走 NotifyChanged()（只触发事件不落盘），确认时 Save() 落盘。
/// </summary>
public class SettingsSaveService : IService
{
    private readonly SaveFileDataWriter _dataWriter;
    private readonly string _settingsPath;
    private readonly SettingsData _data;

    public SettingsData SettingsData => _data;

    /// <summary>设置变更事件（实时预览/落盘均触发），AudioService 订阅后 ApplyVolumes。</summary>
    public event Action OnChanged;

    /// <summary>构造注入文件 IO + 设置文件路径（默认 persistentDataPath/Settings/settings.setting）。
    /// 构造时读取已有设置，无文件用默认值。</summary>
    public SettingsSaveService(SaveFileDataWriter dataWriter_, string settingsPath_)
    {
        _dataWriter = dataWriter_;
        _settingsPath = settingsPath_;
        _data = _dataWriter.Read<SettingsData>(_settingsPath) ?? new SettingsData();
    }

    /// <summary>仅触发变更事件（供滑块拖动时实时预览，不写盘）。</summary>
    public void NotifyChanged() => OnChanged?.Invoke();

    /// <summary>落盘并触发变更事件（面板确认/关闭时调用）。</summary>
    public void Save()
    {
        _dataWriter.Write(_settingsPath, _data);
        OnChanged?.Invoke();
    }
}

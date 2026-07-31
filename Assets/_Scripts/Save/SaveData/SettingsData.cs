using System;

/// <summary>
/// 全局设置数据（不进 GameSaveData、不联网）：经 SettingsService 持久化到独立 .setting 文件。
/// </summary>
[Serializable]
public class SettingsData
{
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
}


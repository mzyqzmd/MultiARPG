using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 本地持久玩家身份：以 PlayerPrefs 存一个跨会话稳定的 GUID 作 playerId，
/// 是 Host 单文件存档中 Players 列表按玩家归档/恢复的唯一键。
/// 同时集中管理"实例后缀"（命令行 -saveSuffix / ParrelSync clone），
/// 使双开测试的两个实例既存档目录隔离，又 playerId 互不冲突。
/// </summary>
public static class LocalPlayerId
{
    private static string _cachedSuffix;
    private static string _cachedId;

    /// <summary>实例后缀：优先命令行 -saveSuffix；其次 ParrelSync clone 的 .parrelsyncarg；无则空。缓存一次。</summary>
    public static string InstanceSuffix
    {
        get
        {
            if (_cachedSuffix != null) return _cachedSuffix;
            _cachedSuffix = ResolveSuffix();
            return _cachedSuffix;
        }
    }

    /// <summary>本地持久 GUID（按实例后缀分键，避免双开共享 PlayerPrefs 时冲突）。首次访问时生成并落盘。</summary>
    public static string Value
    {
        get
        {
            if (!string.IsNullOrEmpty(_cachedId)) return _cachedId;
            string key = string.IsNullOrEmpty(InstanceSuffix) ? "LocalPlayerId" : $"LocalPlayerId_{InstanceSuffix}";
            _cachedId = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(_cachedId))
            {
                _cachedId = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(key, _cachedId);
                PlayerPrefs.Save();
            }
            return _cachedId;
        }
    }

    private static string ResolveSuffix()
    {
        // 1. 命令行显式指定（两个独立 build / 双开编辑器时）：-saveSuffix=Host
        foreach (var arg in Environment.GetCommandLineArgs())
            if (arg.StartsWith("-saveSuffix=", StringComparison.OrdinalIgnoreCase))
                return arg.Substring("-saveSuffix=".Length).Trim();
        // 2. ParrelSync clone：项目根含 .clone 标识，取其 .parrelsyncarg 内容（如 client_0）作后缀
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string cloneMark = Path.Combine(projectRoot, ".clone");
        string argFile = Path.Combine(projectRoot, ".parrelsyncarg");
        if (File.Exists(cloneMark) && File.Exists(argFile))
            return File.ReadAllText(argFile).Trim();
        return string.Empty;
    }
}

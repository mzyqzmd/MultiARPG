using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 数据库：集中管理 UI 预制体引用（仿 SO_AudioDatabase 模式）。
/// BasicCanvas 为分层画布基础设施预制体；面板字典按 key 索引具体面板预制体，
/// 由 UIService 统一按 key 实例化，避免代码硬编码构建 UI 层级。
/// </summary>
[CreateAssetMenu(fileName = "UIDatabase", menuName = "MultiARPG/UI/UIDatabase")]
public class SO_UIDatabase : SerializedScriptableObject
{
    [Header("分层画布基础设施预制体")]
    [SerializeField] private Canvas _basicCanvasPrefab;

    [Header("UI 面板预制体")]
    [DictionaryDrawerSettings(KeyLabel = "面板 Key", ValueLabel = "面板预制体")]
    [OdinSerialize] private readonly Dictionary<string, UI_BaseView> _uiPrefabs = new();

    public Canvas BasicCanvasPrefab => _basicCanvasPrefab;

    public UI_BaseView GetUIPrefab(string key_) =>
        _uiPrefabs.TryGetValue(key_, out var prefab) ? prefab : null;
}

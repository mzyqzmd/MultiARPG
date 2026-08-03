using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品数据库（仿 SO_UIDatabase 模式）：以物品 id（SO_BaseItem._itemID）为 key 集中管理物品预制体来源。
/// 由用户在 Inspector 注册（资产创建 + 填 id），代码仅经 GetItemPrefab 读取。
/// 用 id 而非槽位作 key，使同一槽位（如右手）可注册多把武器。
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "MultiARPG/Item/ItemDatabase")]
public class SO_ItemDatabase : SerializedScriptableObject
{
    [Header("物品预制体（key = 物品 id）")]
    [DictionaryDrawerSettings(KeyLabel = "物品 id", ValueLabel = "物品预制体")]
    [OdinSerialize] private readonly Dictionary<string, BaseItem> _items = new();

    /// <summary>取指定 id 的物品预制体（未注册返回 null）。</summary>
    public BaseItem GetItemPrefab(string id_) =>
        _items.TryGetValue(id_, out var prefab) && prefab != null ? prefab : null;
}

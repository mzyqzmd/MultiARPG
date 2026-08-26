using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局效果注册表（SerializedScriptableObject）：以 OdinSerialize Dictionary&lt;string, BaseEffect&gt; 集中管理所有效果原型。
/// key 为纯字符串 id，建议带领域前缀（domain.entity.variant，如 weapon.rapier.damage / player.heal_small / enemy.zombie.attack），
/// 按 key 字母序在 Inspector 字典内自然分组。运行时由 EffectService.GetEffect(id) 查表并深拷贝返回独立实例。
/// </summary>
[CreateAssetMenu(fileName = "EffectDatabase", menuName = "MultiARPG/Effect/EffectDatabase")]
public class SO_EffectDatabase : SerializedScriptableObject
{
    [Header("效果注册表")]
    [DictionaryDrawerSettings(KeyLabel = "效果 ID", ValueLabel = "效果原型")]
    [OdinSerialize] private readonly Dictionary<string, BaseEffect> _effects = new();

    /// <summary>按 id 查表返回原型实例（共享只读，调用方需自行深拷贝）。</summary>
    public BaseEffect GetPrototype(string id_) =>
        _effects.TryGetValue(id_, out var effect) ? effect : null;
}

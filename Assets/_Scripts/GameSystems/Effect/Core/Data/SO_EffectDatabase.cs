using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

/// <summary>效果数据库：集中注册所有效果资产，按 EffectId 查表，由 EffectService 提供全局查询。</summary>
[CreateAssetMenu(fileName = "EffectDatabase", menuName = "MultiARPG/Effects/EffectDatabase")]
public class SO_EffectDatabase : SerializedScriptableObject
{
    [DictionaryDrawerSettings(KeyLabel = "效果 Id", ValueLabel = "效果资产")]
    [OdinSerialize] private readonly Dictionary<string, SO_BaseEffect> _effects = new();

    public SO_BaseEffect GetEffect(string id_) =>
        _effects.TryGetValue(id_, out var effect) ? effect : null;
}

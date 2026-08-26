using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ModifiableStat_Int
{
    private readonly int _baseValue;
    private readonly List<StatModifier> _modifiers = new();

    public ModifiableStat_Int(int baseValue_) => _baseValue = baseValue_;

    /// <summary>修正后的当前值。</summary>
    public int Value
    {
        get
        {
            float additive = 0;
            float multiplier = 1f;
            foreach (var modifier in _modifiers)
            {
                additive += modifier.additive;
                multiplier *= modifier.multiplier;
            }
            // 四舍五入
            return Mathf.RoundToInt((_baseValue + additive) * multiplier);
        }
    }

    public void Add(StatModifier modifier_) => _modifiers.Add(modifier_);
    /// <summary>按值移除首条匹配修正（施加与移除来自同一效果资产，值必然一致）。</summary>
    public void Remove(StatModifier modifier_) => _modifiers.Remove(modifier_);
}

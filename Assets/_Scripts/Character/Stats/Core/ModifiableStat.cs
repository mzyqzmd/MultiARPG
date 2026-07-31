using System.Collections.Generic;

/// <summary>
/// 可修正属性：持有"基础值 + 生效中修正列表"，当前值 = (基础值 + Σ加值) × Π乘值。
/// </summary>
public class ModifiableStat
{
    private readonly float _baseValue;
    private readonly List<StatModifier> _modifiers = new();

    public ModifiableStat(float baseValue_) => _baseValue = baseValue_;

    /// <summary>修正后的当前值。</summary>
    public float Current
    {
        get
        {
            float additive = 0f, multiplier = 1f;
            foreach (var modifier in _modifiers)
            {
                additive += modifier.additive;
                multiplier *= modifier.multiplier;
            }
            return (_baseValue + additive) * multiplier;
        }
    }

    public void Add(StatModifier modifier_) => _modifiers.Add(modifier_);
    /// <summary>按值移除首条匹配修正（施加与移除来自同一效果资产，值必然一致）。</summary>
    public void Remove(StatModifier modifier_) => _modifiers.Remove(modifier_);
}

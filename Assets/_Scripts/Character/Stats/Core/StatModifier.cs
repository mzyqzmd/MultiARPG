using System;

/// <summary>
/// 单条属性修正：加减用 additive（减=负数），乘除用 multiplier（除=倒数，1=无影响）。
/// 效果经 EffectControl 在本端（owner 本地）直接施加/移除，本结构不实现 INetworkSerializable。
/// 按值移除由 ModifiableStat.Remove 借助 List.Remove 的值类型默认逐字段比较完成（施加与移除来自同一效果资产，值必然一致）。
/// </summary>
[Serializable]
public struct StatModifier
{
    public float additive;
    public float multiplier;

    public StatModifier(float additive_, float multiplier_)
    {
        additive = additive_;
        multiplier = multiplier_;
    }
}

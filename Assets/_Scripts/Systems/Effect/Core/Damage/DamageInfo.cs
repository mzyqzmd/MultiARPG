using UnityEngine;

/// <summary>运行时伤害结算结果（readonly struct，零分配、不写盘）：由攻击结算入口（Character.DealDamage）用攻击者实时属性直接构造，经 IEffectTarget.TakeDamage 专用通道作用到目标。ContactPoint 供后续流血特效定位。不持有任何配置项（poise 按 YAGNI 不纳入）。</summary>
public readonly struct DamageInfo
{
    public readonly int physical;
    public readonly int magic;
    public readonly int fire;
    public readonly int ice;
    public readonly int lightning;

    /// <summary>五类伤害汇总为单一整数总伤害（生命扣减用）。</summary>
    public int TotalDamage => physical + magic + fire + ice + lightning;

    public readonly Vector3 contactPoint;
    public readonly IEffectSource source;
    public readonly IEffectTarget target;

    public DamageInfo(int physical_, int magic_, int fire_, int ice_, int lightning_,
        Vector3 contactPoint_, IEffectSource source_, IEffectTarget target_)
    {
        physical = physical_;
        magic = magic_;
        fire = fire_;
        ice = ice_;
        lightning = lightning_;
        contactPoint = contactPoint_;
        source = source_;
        target = target_;
    }
}

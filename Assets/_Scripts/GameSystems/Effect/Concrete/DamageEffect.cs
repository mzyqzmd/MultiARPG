using System;
using UnityEngine;
[Serializable]
public class DamageEffect : BaseEffect
{
    [Tooltip("物理伤害")]
    [SerializeField] private int _physicalDamage;
    [Tooltip("魔法伤害")]
    [SerializeField] private int _magicalDamage;

    public int PhysicalDamage => _physicalDamage;
    public int MagicDamage => _magicalDamage;

    /// <summary>纯函数：返回已乘倍率的最终物理/魔法伤害整数（标准四舍五入，.5 远离零方向，避免银行家舍入的偶数偏好导致 1.5× 系列伤害少 1）。</summary>
    public (int physical, int magic) Compute(float multiplier_) =>
        ((int)Math.Round(_physicalDamage * multiplier_, MidpointRounding.AwayFromZero),
         (int)Math.Round(_magicalDamage * multiplier_, MidpointRounding.AwayFromZero));

    public override void Execute(IEffectTarget target_, IEffectSource source_)
    {
        float multiplier = source_?.DamageMultiplier ?? 1f;
        (int physical, int magic) = Compute(multiplier);
        target_.TakeDamage(physical, magic);
    }
}

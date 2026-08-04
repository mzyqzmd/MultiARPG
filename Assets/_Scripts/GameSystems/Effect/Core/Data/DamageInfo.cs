using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>运行时伤害结算结果（普通 struct，可经 RPC 跨端传输）：由效果来源（IEffectSource，如武器 SO_DamageEffect）在构造时直接填入五类伤害数值，经 IEffectTarget.TakeDamage 作用到目标（CharacterStatsControl）。ContactPoint 供后续流血特效定位。Source/Target 为引用类型不参与 RPC 序列化（命中时由 NetworkObjectId 定位），仅序列化五类伤害 + ContactPoint，为未来抗性/伤害类型区分预留完整数据。</summary>
public struct DamageInfo : INetworkSerializable
{
    public int physical;
    public int magic;
    public int fire;
    public int ice;
    public int lightning;

    /// <summary>五类伤害汇总为单一整数总伤害（生命扣减用）。</summary>
    public readonly int TotalDamage => physical + magic + fire + ice + lightning;

    public Vector3 contactPoint;
    public IEffectSource source;
    public IEffectTarget target;

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

    /// <summary>从效果集合聚合五类伤害（仅累加 SO_DamageEffect，忽略非伤害效果），供武器侧一次性构建伤害。保持与"武器仅传伤害"语义一致。</summary>
    public static DamageInfo FromEffects(IEnumerable<SO_BaseEffect> effects_, Vector3 contactPoint_,
        IEffectSource source_, IEffectTarget target_)
    {
        int p = 0, m = 0, f = 0, i = 0, l = 0;
        foreach (var effect in effects_)
        {
            if (effect is SO_DamageEffect dmg)
            {
                p += dmg.PhysicalDamage;
                m += dmg.MagicDamage;
                f += dmg.FireDamage;
                i += dmg.IceDamage;
                l += dmg.LightningDamage;
            }
        }
        return new DamageInfo(p, m, f, i, l, contactPoint_, source_, target_);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> s_) where T : IReaderWriter
    {
        s_.SerializeValue(ref physical);
        s_.SerializeValue(ref magic);
        s_.SerializeValue(ref fire);
        s_.SerializeValue(ref ice);
        s_.SerializeValue(ref lightning);
        s_.SerializeValue(ref contactPoint);
    }
}

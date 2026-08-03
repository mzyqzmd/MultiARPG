using UnityEngine;

/// <summary>接触型伤害效果（武器 / 陷阱 / 熔岩 / 毒云等）：伤害数值写死在资产字段。构造 DamageInfo 交给调用方（如 Weapon_Rapier）经 Character.RequestDamageServerRpc → Host 中转 → 目标 owner 本地 TakeDamage 落地扣血，仅数值来源不同（写死 vs 攻击者实时属性）。</summary>
[CreateAssetMenu(fileName = "SO_DamageEffect", menuName = "MultiARPG/Effects/DamageEffect")]
public class SO_DamageEffect : SO_BaseEffect
{
    [Tooltip("物理伤害")]
    [SerializeField] private int _physicalDamage;
    [Tooltip("魔法伤害")]
    [SerializeField] private int _magicalDamage;
    [Tooltip("火焰伤害")]
    [SerializeField] private int _fireDamage;
    [Tooltip("冰霜伤害")]
    [SerializeField] private int _iceDamage;
    [Tooltip("雷电伤害")]
    [SerializeField] private int _lightningDamage;

    public int PhysicalDamage => _physicalDamage;
    public int MagicDamage => _magicalDamage;
    public int FireDamage => _fireDamage;
    public int IceDamage => _iceDamage;
    public int LightningDamage => _lightningDamage;

    public override void Execute(IEffectTarget target_, IEffectSource source_)
    {
        target_.TakeDamage(new DamageInfo(_physicalDamage, _magicalDamage, _fireDamage, _iceDamage, _lightningDamage,
            Vector3.zero, source_, target_));
    }
}

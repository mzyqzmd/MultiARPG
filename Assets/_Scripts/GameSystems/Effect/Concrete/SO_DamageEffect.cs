using UnityEngine;

/// <summary>接触型伤害效果（武器 / 陷阱 / 熔岩 / 毒云等）：伤害数值写死在资产字段。</summary>
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

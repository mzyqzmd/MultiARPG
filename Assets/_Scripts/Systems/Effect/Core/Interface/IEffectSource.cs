/// <summary>效果攻击者来源：暴露五类攻击属性（int），供攻击结算（Character.DealDamage）直接读取构造 DamageInfo。CharacterStatsControl 提供默认 0 实现，PlayerStatsControl override 读 SO_PlayerStats。</summary>
public interface IEffectSource
{
    int PhysicalAttack { get; }
    int MagicAttack { get; }
    int FireAttack { get; }
    int IceAttack { get; }
    int LightningAttack { get; }
}

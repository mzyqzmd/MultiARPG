/// <summary>效果来源身份标记接口：仅标识「谁发起效果 / 伤害」，不携带任何攻击数值。
/// 实际伤害数值（五类 attack）由来源方在构造 DamageInfo 时直接填入，不读本接口属性（2026-08-01 决策：攻击属性归武器 Item，角色不持有）。</summary>
public interface IEffectSource
{
}

/// <summary>效果作用目标：任何可被施加伤害 / 回复 / 属性修正等效果的对象（如 Character）实现此接口。</summary>
public interface IEffectTarget
{
    /// <summary>受伤：接收运行时结算出的 DamageInfo（含元素类型与总伤害），由实现方提取 TotalDamage 扣血。</summary>
    void TakeDamage(DamageInfo damage_);
    void Heal(int amount_);
    /// <summary>施加一个效果：由实现方转发到自身 EffectControl，并带入攻击者来源（环境伤害 source_ 为 null）。</summary>
    void ApplyEffect(SO_BaseEffect effect_, IEffectSource source_);
    void ApplyStatModifier(EStatType type_, StatModifier modifier_);
    void RemoveStatModifier(EStatType type_, StatModifier modifier_);
}

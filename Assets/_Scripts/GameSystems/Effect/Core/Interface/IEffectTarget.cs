using System.Collections.Generic;


/// <summary>效果作用目标：任何可被施加伤害 / 回复 / 属性修正等效果的数据持有者（如 CharacterStatsControl）实现此接口。
/// 所有效果施加统一经 EffectControl.ApplyEffect 入口（含 Instant/Timed/Static），外部调用方不应绕过 EffectControl 直接调 Execute。</summary>
public interface IEffectTarget
{
    /// <summary>统一效果施加入口：外部调用方经此属性 ApplyEffect，由 EffectControl 内部按 EffectType 分流（Instant 快速路径 / Timed 池化）。</summary>
    EffectControl EffectControl { get; }
    public void TakeDamage(int physical_, int magic_);
    public void Heal(int amount_);
    public void ApplyStatModifier(EStatType type_, StatModifier modifier_);
    public void RemoveStatModifier(EStatType type_, StatModifier modifier_);
}

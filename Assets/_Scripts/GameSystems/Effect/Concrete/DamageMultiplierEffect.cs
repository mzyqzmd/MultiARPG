using UnityEngine;

/// <summary>
/// 伤害倍率效果（普通类，非 ScriptableObject）。施加时把来源（攻击者）的 IEffectSource.DamageMultiplier 设为指定倍率，
/// 到期（Timed 类型）自动 OnRemove 复位为 1。用于在蓄力足够久时为武器临时附加更高伤害，时间到前打出即生效。
/// 配置：在 Inspector 将 EffectType 设为 Timed 并填 Duration（持续秒数），_multiplier 填倍率（如 1.5 / 2.5）。
/// </summary>
[System.Serializable]
public class DamageMultiplierEffect : BaseEffect
{
    [Tooltip("伤害倍率，如 1.5 表示 150% 伤害。仅 Timed 类型在 Duration 到点前生效。")]
    [SerializeField] private float _multiplier = 1f;

    private float _originalMultiplier;

    public override void Execute(IEffectTarget target_, IEffectSource source_)
    {
        _originalMultiplier = source_.DamageMultiplier;
        source_.DamageMultiplier = _multiplier;
    }

    public override void OnRemove(IEffectTarget target_, IEffectSource source_) =>
        source_.DamageMultiplier = _originalMultiplier;
}

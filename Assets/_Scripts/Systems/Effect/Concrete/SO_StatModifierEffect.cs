using UnityEngine;

/// <summary>
/// 属性修正效果（通用）：对目标（CharacterStatsControl）的白名单属性施加一条加减乘除修正，移除时自动回滚。
/// 用法示例（建资产配置即可，无需新类）：减速 = NormalMoveSpeed, additive=0, multiplier=0.7, Timed 5s；
/// 血量上限增益 = MaxHealth, additive=20, multiplier=1, Static。
/// </summary>
[CreateAssetMenu(fileName = "StatModifierEffect", menuName = "MultiARPG/Effects/StatModifierEffect")]
public class SO_StatModifierEffect : SO_BaseEffect
{
    [Tooltip("要修正的属性（白名单枚举，玩家专属属性对 Enemy 施加会被静默忽略）。")]
    [SerializeField] private EStatType _statType;
    [Tooltip("加值：正数加、负数减（在乘除之前参与计算）。")]
    [SerializeField] private float _additive = 0f;
    [Tooltip("乘值：>1 增益、<1 减益（除法配倒数），1 = 无影响。")]
    [SerializeField] private float _multiplier = 1f;

    public override void Execute(IEffectTarget target_, IEffectSource _) =>
        target_.ApplyStatModifier(_statType, new StatModifier(_additive, _multiplier));

    public override void OnRemove(IEffectTarget target_, IEffectSource _) =>
        target_.RemoveStatModifier(_statType, new StatModifier(_additive, _multiplier));
}

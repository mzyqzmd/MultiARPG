using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>武器专用数据：在通用物品字段之外，额外承载蓄力重击倍率配置与武器专属动画覆盖控制器。</summary>
[CreateAssetMenu(fileName = "Weapon", menuName = "MultiARPG/Item/Weapon")]
public class SO_Weapon : SO_BaseItem
{
    [Header("蓄力重击倍率")]
    [Tooltip("短蓄力（达到短阈值但未满）时的伤害倍率，如 1.5 表示 150% 伤害。")]
    [SerializeField] private float _shortChargeMulti = 1.5f;
    [Tooltip("长蓄力（满蓄）时的伤害倍率，如 2.5 表示 250% 伤害。")]
    [SerializeField] private float _longChargeMulti = 2.5f;

    [Header("武器动画")]
    [Tooltip("武器动画覆盖控制器：override 基础 AC_Player 中攻击/装备等状态的 clip，其余状态自动继承基础。装备时赋给玩家 Animator.runtimeAnimatorController，脱装备还原基础控制器。")]
    [SerializeField] private AnimatorOverrideController _overrideController;

    /// <summary>短蓄力倍率。</summary>
    public float ShortChargeMulti => _shortChargeMulti;
    /// <summary>长蓄力倍率。</summary>
    public float LongChargeMulti => _longChargeMulti;
    /// <summary>武器动画覆盖控制器。</summary>
    public AnimatorOverrideController OverrideController => _overrideController;
}

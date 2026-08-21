using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>武器专用数据：独立于消耗品（SO_BaseItem）体系，自带可装备契约字段与蓄力重击倍率、武器专属动画覆盖控制器。</summary>
[CreateAssetMenu(fileName = "Weapon", menuName = "MultiARPG/Weapon")]
public class SO_Weapon : SerializedScriptableObject
{
    [Tooltip("武器唯一标识：自动同步为资产文件名。")]
    [Header("ID")]
    [SerializeField] private string _weaponID;
    [Header("图标")]
    [SerializeField] private Sprite _weaponSprite;
    [Header("描述")]
    [TextArea(3, 10), SerializeField] protected string _weaponDescription;
    [Header("被装备上时需要挂载的槽位；IsOccupying=false 仅表现挂不占槽。")]
    [SerializeField] private EquipSlot[] _equipTargetSlots = new[] { new EquipSlot(EEquipmentSlot.RightHand, true) };
    [Header("被卸下时需要挂载的槽位；IsOccupying=false 仅表现挂不占槽。")]
    [SerializeField] private EquipSlot[] _unequipTargetSlots = new[] { new EquipSlot(EEquipmentSlot.LeftHip, true) };
    [Header("武器命中时触发的效果（通常为 SO_DamageEffect 伤害）。")]
    [SerializeField] private List<SO_BaseEffect> _effects = new();

    [Header("蓄力重击倍率")]
    [Tooltip("短蓄力（达到短阈值但未满）时的伤害倍率，如 1.5 表示 150% 伤害。")]
    [SerializeField] private float _shortChargeMulti = 1.5f;
    [Tooltip("长蓄力（满蓄）时的伤害倍率，如 2.5 表示 250% 伤害。")]
    [SerializeField] private float _longChargeMulti = 2.5f;

    [Header("武器动画")]
    [Tooltip("武器动画覆盖控制器：override 基础 AC_Player 中攻击/装备等状态的 clip，其余状态自动继承基础。装备时赋给玩家 Animator.runtimeAnimatorController，脱装备还原基础控制器。")]
    [SerializeField] private AnimatorOverrideController _overrideController;

    /// <summary>装备槽位配置（实例类据此实现 IEquippable）。</summary>
    public EquipSlot[] EquipTargetSlots => _equipTargetSlots;
    /// <summary>卸下槽位配置。</summary>
    public EquipSlot[] UnequipTargetSlots => _unequipTargetSlots;
    /// <summary>武器唯一标识。</summary>
    public string ItemID => _weaponID;
    /// <summary>武器图标（快捷栏展示用）。</summary>
    public Sprite ItemSprite => _weaponSprite;
    /// <summary>武器命中时触发的效果集合。</summary>
    public IReadOnlyList<SO_BaseEffect> Effects => _effects;
    /// <summary>短蓄力倍率。</summary>
    public float ShortChargeMulti => _shortChargeMulti;
    /// <summary>长蓄力倍率。</summary>
    public float LongChargeMulti => _longChargeMulti;
    /// <summary>武器动画覆盖控制器。</summary>
    public AnimatorOverrideController OverrideController => _overrideController;

    protected void OnValidate() => _weaponID = name;
}

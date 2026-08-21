using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BaseItem", menuName = "MultiARPG/Item/BaseItem")]
public abstract class SO_BaseItem : SerializedScriptableObject
{
    [Tooltip("物品唯一标识：自动同步为资产文件名。")]
    [Header("ID")]
    [SerializeField] protected string _itemID;
    [Header("图标")]
    [SerializeField] protected Sprite _itemSprite;
    [Header("描述")]
    [TextArea(3, 10), SerializeField] protected string _itemDescription;
    [Header("被装备上时需要挂载的槽位；IsOccupying=false 仅表现挂不占槽。")]
    [SerializeField] protected EquipSlot[] _equipTargetSlots = new[] { new EquipSlot(EEquipmentSlot.RightHand, true) };
    [Header("被卸下时需要挂载的槽位；IsOccupying=false 仅表现挂不占槽。")]
    [SerializeField] protected EquipSlot[] _unequipTargetSlots = new[] { new EquipSlot(EEquipmentSlot.LeftHip, true) };
    [Header("物品被使用时触发的效果（药壶治疗、陷阱伤害等）。")]
    [SerializeField] protected List<SO_BaseEffect> _effects = new();

    /// <summary>装备槽位配置（实例类据此实现 IEquippable）。</summary>
    public EquipSlot[] EquipTargetSlots => _equipTargetSlots;
    /// <summary>卸下槽位配置。</summary>
    public EquipSlot[] UnequipTargetSlots => _unequipTargetSlots;
    /// <summary>物品唯一标识。</summary>
    public string ItemID => _itemID;
    /// <summary>物品图标（装备面板/快捷栏展示用）。</summary>
    public Sprite ItemSprite => _itemSprite;
    /// <summary>被使用时触发的效果集合（药壶的治疗来源等）。</summary>
    public IReadOnlyList<SO_BaseEffect> Effects => _effects;

    protected void OnValidate() => _itemID = name;
}

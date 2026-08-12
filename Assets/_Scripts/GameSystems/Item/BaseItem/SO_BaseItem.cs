using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BaseItem", menuName = "MultiARPG/Item/BaseItem")]
public class SO_BaseItem : SerializedScriptableObject
{
    [Tooltip("物品唯一标识：自动同步为资产文件名。")]
    [Header("ID")]
    [SerializeField] protected string _itemID;
    [Header("图标")]
    [SerializeField] protected Sprite _itemSprite;
    [Header("描述")]
    [TextArea(3, 10), SerializeField] protected string _itemDescription;
    [Header("类型")]
    [SerializeField] protected EItemType _itemType;
    [Header("被装备上时需要挂载的槽位(类型为材料时则不启用)；IsOccupying=false 仅表现挂不占槽。")]
    [EnableIf("@_itemType != EItemType.Material"), SerializeField]
    protected EquipSlot[] _equipTargetSlots = new[] { new EquipSlot(EEquipmentSlot.RightHand, true) };
    [Header("被卸下时需要挂载的槽位(类型为材料时则不启用)；IsOccupying=false 仅表现挂不占槽。")]
    [EnableIf("@_itemType != EItemType.Material"), SerializeField]
    protected EquipSlot[] _unequipTargetSlots = new[] { new EquipSlot(EEquipmentSlot.LeftHip, true) };
    [Header("改物品被使用时触发的效果(类型为材料时则不启用)")]
    [EnableIf("@_itemType != EItemType.Material"), SerializeField]
    protected List<SO_BaseEffect> _effects = new();

    public EquipSlot[] EquipTargetSlots => _equipTargetSlots;
    public EquipSlot[] UnequipTargetSlots => _unequipTargetSlots;
    /// <summary>物品图标（装备面板/快捷栏展示用）。</summary>
    public Sprite ItemSprite => _itemSprite;
    /// <summary>被使用时触发的效果集合（武器/陷阱的伤害来源、药壶的治疗来源等）。</summary>
    public IReadOnlyList<SO_BaseEffect> Effects => _effects;

    protected void OnValidate() => _itemID = name;

    /// <summary>武器/药壶等子类 override 实现具体行为。</summary>
    public void ApplyItem(IEffectTarget target_, IEffectSource source_)
    {
        foreach (var effect in _effects)
            effect.Execute(target_, source_);
    }
}

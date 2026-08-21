using UnityEngine;

/// <summary>场景中的消耗品实例（药壶/道具）：承载运行时状态（持有者、装备态），并持有一个对应的 SO_BaseItem 数据引用。
/// 武器不继承此类，独立成 BaseWeapon 线。</summary>
public abstract class BaseItem : MonoBehaviour, IEquippable
{
    [SerializeField] protected SO_BaseItem _itemData;
    [SerializeField, Tooltip("物品持有者。不注册则作为无来源")]
    protected Player _owner;

    public SO_BaseItem ItemData => _itemData;
    public Player Owner => _owner;
    //IEquippable接口实现
    public string ItemID => _itemData.ItemID;
    public Sprite ItemSprite => _itemData.ItemSprite;
    public bool IsEquipped { get; set; }
    public EquipSlot[] EquipTargetSlots => _itemData.EquipTargetSlots;
    public EquipSlot[] UnequipTargetSlots => _itemData.UnequipTargetSlots;
    public void RegisterOwner(Player owner_) => _owner = owner_;

    /// <summary>使用物品：遍历数据上的效果集合逐个结算（药壶治疗等）。武器不调用此方法。</summary>
    public virtual void ApplyItem(IEffectTarget target_, IEffectSource source_)
    {
        foreach (var effect in _itemData.Effects)
            effect.Execute(target_, source_);
    }
}

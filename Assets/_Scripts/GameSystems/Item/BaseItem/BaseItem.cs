using UnityEngine;

/// <summary>场景中的物品实例：承载跨子类共享的运行时状态（持有者、装备态），并持有一个对应的 SO_BaseItem 数据引用。</summary>
public abstract class BaseItem : MonoBehaviour
{
    [SerializeField] protected SO_BaseItem _itemData;
    [SerializeField, Tooltip("武器持有者（联机时注册攻击者）。不注册则作为无来源环境伤害")]
    protected Player _owner;

    public Player Owner => _owner;
    public SO_BaseItem ItemData => _itemData;
    /// <summary>武器数据转型（非武器物品为 null）。</summary>
    public SO_Weapon WeaponData => _itemData as SO_Weapon;
    public bool IsEquipped { get; set; }

    public void RegisterOwner(Player owner_) => _owner = owner_;

    public virtual void ApplyItem(IEffectTarget target_, IEffectSource source_) =>
        _itemData.ApplyItem(target_, source_);

    /// <summary>重击/特殊使用入口（SpecialApply 键）。默认空实现，武器子类 override 触发重击态，盾牌/材料不实现即无重击。</summary>
    public virtual void SpecialApply(Player player_) { }
}

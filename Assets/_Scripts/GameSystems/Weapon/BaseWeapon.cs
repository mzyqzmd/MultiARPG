using UnityEngine;

/// <summary>场景中的武器实例：承载运行时状态（持有者、装备态），持有 SO_Weapon 数据引用。
/// 与消耗品（BaseItem）互不继承，各自实现 IEquippable 接入装备系统。
/// 攻击命中碰撞体默认禁用，由动画事件经 Enable/DisableAttackCollider 在攻击窗口期启停（非攻击期不结算）。</summary>
public abstract class BaseWeapon : MonoBehaviour, IEquippable
{
    [SerializeField] protected SO_Weapon _weaponData;
    [SerializeField] protected Collider _attackCollider;
    [SerializeField, Tooltip("物品持有者。不注册则作为无来源")]
    protected Player _owner;

    /// <summary>武器数据引用。</summary>
    public SO_Weapon WeaponData => _weaponData;
    public Player Owner => _owner;

    #region IEquippable接口实现
    public string ItemID => _weaponData.ItemID;
    public Sprite ItemSprite => _weaponData.ItemSprite;
    public bool IsEquipped { get; set; }
    public EquipSlot[] EquipTargetSlots => _weaponData.EquipTargetSlots;
    public EquipSlot[] UnequipTargetSlots => _weaponData.UnequipTargetSlots;
    #endregion

    public void RegisterOwner(Player owner_) => _owner = owner_;

    #region 启用/禁用攻击碰撞体
    /// <summary>启用武器碰撞体（攻击命中窗口期由动画事件驱动开启）。</summary>
    public void EnableAttackCollider() => _attackCollider.enabled = true;
    /// <summary>禁用武器碰撞体（非攻击期关闭，避免走路蹭到目标误伤）。</summary>
    public void DisableAttackCollider() => _attackCollider.enabled = false;
    #endregion
}

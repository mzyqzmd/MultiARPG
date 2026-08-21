using UnityEngine;

/// <summary>场景中的武器实例：承载运行时状态（持有者、装备态），持有 SO_Weapon 数据引用。
/// 与消耗品（BaseItem）互不继承，各自实现 IEquippable 接入装备系统。</summary>
public abstract class BaseWeapon : MonoBehaviour, IEquippable
{
    [SerializeField] protected SO_Weapon _weaponData;
    [SerializeField] protected Collider _attackCollider;
    [SerializeField, Tooltip("物品持有者。不注册则作为无来源")]
    protected Player _owner; 

    /// <summary>武器数据引用。</summary>
    public SO_Weapon WeaponData => _weaponData;
    public Player Owner => _owner;
    //IEquippable接口实现
    public string ItemID => _weaponData.ItemID;
    public Sprite ItemSprite => _weaponData.ItemSprite;
    public bool IsEquipped { get; set; }
    /// <summary>当前蓄力倍率载体：由 PlayerHeavyAttackState 退出蓄力时按分级写入（太短=1、短蓄=ShortChargeMulti、满蓄=LongChargeMulti）；命中聚合后复位为 1 防残留。</summary>
    public EquipSlot[] EquipTargetSlots => _weaponData.EquipTargetSlots;
    public EquipSlot[] UnequipTargetSlots => _weaponData.UnequipTargetSlots;
    public void RegisterOwner(Player owner_) => _owner = owner_;
    public float CurrentMultiplier { get; set; } = 1f;

    /// <summary>启用武器碰撞体（攻击命中窗口期由动画事件驱动开启）。</summary>
    public void EnableAttackCollider() => _attackCollider.enabled = true;
    /// <summary>禁用武器碰撞体（非攻击期关闭，避免走路蹭到目标误伤）。</summary>
    public void DisableAttackCollider() => _attackCollider.enabled = false;

    /// <summary>场景武器拾取（纯本地，各端各自捡各自副本，不抢武器）：未装备状态下碰到本地玩家（IsOwner）时
    /// 按 EquipTargetSlots/UnequipTargetSlots 空闲情况即时装备或收起挂背，两者均无空间则不拾取；
    /// 成功后关闭攻击碰撞体（非攻击期防误伤，攻击窗口由动画事件重新启用）。
    /// 已装备时跳过拾取，交由子类伤害结算（互斥）。</summary>
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (IsEquipped) return;
        var player = other.GetComponentInParent<Player>();
        if (player == null || !player.IsOwner) return;
        if (!player.EquipmentControl.TryPickup(this)) return;
        DisableAttackCollider();
    }
}

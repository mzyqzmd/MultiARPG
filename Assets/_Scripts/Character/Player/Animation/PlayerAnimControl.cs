using System;
using UnityEngine;

/// <summary>装备切换动画控制（MonoBehaviour，挂 Player 预制体）：仅持有 Animator，负责播"拔出/收起"动画并暴露三个 Animation Event 出口，
/// 不含任何业务逻辑（物品/装备/状态机/物品服务均不依赖），由 PlayerEquipmentControl 经 [SerializeField] 自订阅编排。</summary>
public class PlayerAnimControl : MonoBehaviour
{
    [Header("玩家引用")]
    [SerializeField] private Player _player;

    public void OnToggleFrameEvent()
    {
        if (_player.EquipmentControl.PendingItem == null) return;
        if (_player.EquipmentControl.IsSlotTaken(_player.EquipmentControl.PendingSlot))
            _player.EquipmentControl.Equip(_player.EquipmentControl.PendingItem);
        else
            _player.EquipmentControl.Unequip(_player.EquipmentControl.PendingItem);
        _player.EquipmentControl.PendingItem = null;
    }

    #region 轻击连击窗口
    /// <summary>Animation Clip 事件：轻击连击窗口开启，转发至状态机当前轻击态。</summary>
    public void OnComboWindowOpen() => _player.StateControl.OpenComboWindow();
    /// <summary>Animation Clip 事件：轻击段结束（窗口关闭），转发至状态机当前轻击态。</summary>
    public void OnComboWindowClose() => _player.StateControl.CloseComboWindow();
    #endregion

    #region 攻击碰撞体窗口
    /// <summary>Animation Clip 事件：攻击命中窗口开启，启用右手武器碰撞体。</summary>
    public void OnAttackColliderOpen() => _player.StateControl.EnableWeaponCollider();
    /// <summary>Animation Clip 事件：攻击命中窗口关闭，禁用右手武器碰撞体。</summary>
    public void OnAttackColliderClose() => _player.StateControl.DisableWeaponCollider();
    #endregion

    /// <summary>Animation Clip 事件：返回移动状态，转发至状态机当前移动态。</summary>
    public void ReturnToMovementState() => _player.StateControl.ReturnToMovementState();
    /// <summary>Animation Clip 事件：蓄力分级帧，转发 OnHeavyChargeTierResolved（tier: 1=短蓄 2=满蓄）。</summary>
    public void OnHeavyChargeTier(int tier_) => _player.StateControl.EndHeavyChargeByTier(tier_);
}

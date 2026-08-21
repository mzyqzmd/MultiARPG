using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateAnimEventControl
{
    private readonly Player _player;
    private readonly PlayerStateMachineBuilder _builder;
    public PlayerStateAnimEventControl(Player player_, PlayerStateMachineBuilder bilder_)
    {
        _player = player_;
        _builder = bilder_;
    }
    /// <summary>轻击连段窗口开启（动画事件 OnComboWindowOpen 驱动）：置标志，AnyTransition 此刻起允许消费轻击请求连段。</summary>
    public void OpenComboWindow() => _player.StateControl.ComboWindowOpen = true;
    /// <summary>轻击段结束（窗口关闭，动画事件 OnComboWindowClose 驱动）：关标志并清残留请求，避免窗口外误连。</summary>
    public void CloseComboWindow()
    {
        _player.StateControl.ComboWindowOpen = false;
        _player.StateControl.ConsumeNormalAttack();
    }
    /// <summary>动画事件驱动的蓄力结算：按 tier 写倍率到右手武器并扣耐力。tier>=2=满蓄(LongChargeMulti)，否则短蓄(ShortChargeMulti)。不在此切状态——倍率写完后由动画机 IsChargingHeavyAttack==false 自动流转释放动画，释放动画末尾 clip 事件再驱动退出。</summary>
    public void EndHeavyChargeByTier(int tier_)
    {
        if (_player.EquipmentControl.GetEquippedItem(EEquipmentSlot.RightHand) is BaseWeapon weapon)
        {
            _player.StatsControl.SpendStamina(_player.StateControl.HeavyAttackStaminaCost);
            weapon.CurrentMultiplier = tier_ >= 2 ? weapon.WeaponData.LongChargeMulti : weapon.WeaponData.ShortChargeMulti;
            Debug.Log($"Rapier HeavyAttackMultiplier:{weapon.CurrentMultiplier}");
        }
        _player.StateControl.ConsumeHeavyAttack();
    }

    /// <summary>当前攻击段播放完毕（动画事件 OnAttackEnd 驱动）：回到移动态并清标志。
    public void ReturnToMovementState() => _builder.ChildFsm.ChangeState(_builder.MovementState);

    /// <summary>攻击命中窗口开启（动画事件 OnAttackColliderOpen 驱动）：启用右手武器碰撞体，仅窗口期内可命中目标。</summary>
    public void EnableWeaponCollider()
    {
        if (_player.EquipmentControl.GetEquippedItem(EEquipmentSlot.RightHand) is BaseWeapon weapon)
            weapon.EnableAttackCollider();
    }
    /// <summary>攻击命中窗口关闭（动画事件 OnAttackColliderClose 驱动）：禁用右手武器碰撞体，避免非攻击期误伤。</summary>
    public void DisableWeaponCollider()
    {
        if (_player.EquipmentControl.GetEquippedItem(EEquipmentSlot.RightHand) is BaseWeapon weapon)
            weapon.DisableAttackCollider();
    }
}

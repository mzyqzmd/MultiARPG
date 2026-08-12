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
    /// <summary>动画事件驱动的蓄力结算：按 tier 写倍率到右手武器并扣耐力。tier>=2=满蓄(LongChargeMulti)，否则短蓄(ShortChargeMulti)。不在此切状态——倍率写完后由动画机 IsChargingHeavyAttack==false 自动流转释放动画，释放动画末尾 clip 事件再驱动退出。</summary>
    public void EndHeavyChargeByTier(int tier_)
    {
        _player.StatsControl.SpendStamina(_player.StateControl.HeavyAttackStaminaCost);
        if (_player.EquipmentControl.GetEquippedItem(EEquipmentSlot.RightHand) is Weapon_Rapier rapier && rapier.WeaponData != null)
        {
            rapier.CurrentMultiplier = tier_ >= 2 ? rapier.WeaponData.LongChargeMulti : rapier.WeaponData.ShortChargeMulti;
            Debug.Log($"Rapier HeavyAttackMultiplier:{rapier.CurrentMultiplier}");
        }
        _player.StateControl.ConsumeHeavyAttack();
    }
}

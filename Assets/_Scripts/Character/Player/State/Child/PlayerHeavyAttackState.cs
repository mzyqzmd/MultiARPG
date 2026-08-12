using Animancer;
using UnityEngine;

/// <summary>
/// 玩家重击（蓄力）状态：进入播 Heavy_Charge（起手蓄力）→ Charge 结束进 Heavy_Hold（保持蓄满姿态，停留等松开）
/// → 松开重击键播 Heavy_Release（挥出收尾），Release 结束回移动态。
/// 蓄力结算倍率仍由 PlayerStateControl.EndHeavyChargeByTier（动画事件 OnHeavyChargeTier）写入右手武器。
/// 取消（Charge 阶段中途松开）不播释放直接退出；Hold 阶段松开一律走 Release。
/// 动画经 HybridAnimancerComponent 播放覆盖控制器。
/// </summary>
public class PlayerHeavyAttackState : PlayerActionState
{
    private SO_Weapon _weapon;
    private EHeavyPhase _phase;

    public PlayerHeavyAttackState(StateMachine<PlayerActionState> stateMachine_,
        Player player_, CharacterController characterController_) :
        base(stateMachine_, player_, characterController_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        _phase = EHeavyPhase.Charge;
        _weapon = _player.EquipmentControl.GetEquippedItem(EEquipmentSlot.RightHand).ItemData as SO_Weapon;
        _player.StateControl.ConsumeHeavyAttack();
        if (!TryPlay("Heavy_Charge", OnChargeEnd))
            ReturnToMovement();
    }

    public override void Exit()
    {
        base.Exit();
        _player.StateControl.ConsumeHeavyAttack();
        _phase = EHeavyPhase.None;
    }

    /// <summary>松开重击键：Charge 阶段取消(不挥)，Hold 阶段释放(播 Release)。</summary>
    public void OnSpecialReleased()
    {
        if (_phase == EHeavyPhase.Charge)
            ReturnToMovement();
        else if (_phase == EHeavyPhase.Hold)
            Release();
    }

    /// <summary>取消蓄力（Charge 阶段中途松开）：不播释放直接退出。</summary>
    public void CancelCharge()
    {
        if (_phase == EHeavyPhase.Charge)
            ReturnToMovement();
    }

    private void OnChargeEnd()
    {
        _phase = EHeavyPhase.Hold;
        // Hold 播到末帧自动过渡到 Release（不再无限循环等松手）。
        if (!TryPlay("Heavy_Hold", OnHoldEnd))
            Release();
    }

    private void OnHoldEnd() => Release();

    private void Release()
    {
        _phase = EHeavyPhase.Release;
        if (!TryPlay("Heavy_Release", OnReleaseEnd))
            ReturnToMovement();
    }

    private void OnReleaseEnd() => ReturnToMovement();

    /// <summary>退出回移动态：Hybrid 模式用 PlayController 让控制器恢复输出（取代 Stop，避免失去输出卡在末帧），平滑淡回 Locomotion。</summary>
    private void ReturnToMovement()
    {
        _player.Animancer.PlayController();
        _stateMachine.ChangeState(_player.StateControl.MovementState);
    }

    private bool TryPlay(string key_, System.Action onEnd_)
    {
        if (_weapon == null || !_weapon.WeaponAnimations.TryGetValue(key_, out var clip))
            return false;
        var state = _player.Animancer.Play(clip);
        if (state.Events(this, out var events))
            events.OnEnd = onEnd_;
        return true;
    }
}

/// <summary>重击三阶段：起手蓄力 / 保持蓄满 / 释放挥出。</summary>
public enum EHeavyPhase
{
    None,
    Charge,
    Hold,
    Release,
}

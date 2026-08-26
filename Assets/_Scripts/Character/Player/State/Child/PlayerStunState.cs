using UnityEngine;

/// <summary>
/// 击晕动作态：进入后角色停摆（不可移动/闪避/攻击/跳跃），必须等击晕时长完全走完才自动回移动态。
/// 与受击态的区别：不可被闪避（冲刺）抢断脱身；无朝向 2D BlendTree（单一无方向眩晕动画）。
/// 进态解除锁定（破锁），但不冻结输入/相机（玩家仍可转视角观察）。
/// </summary>
public class PlayerStunState : PlayerActionState
{
    public PlayerStunState(StateMachine<PlayerActionState> stateMachine_,
        Player player_, CharacterController characterController_) :
        base(stateMachine_, player_, characterController_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        // 进态破锁：击晕无法维持锁定姿态，与闪避/跳跃一致
        _player.StateControl.RequestLockBreak();
        _player.StateControl.IsBusy = true;
        _player.StateControl.ConsumeStun();
        // 击晕时长由配置（SO_CharacterStats.StunDuration）驱动，可被延长/缩短击晕类效果经 EStatType.StunDuration 修正
        _stateDuration = _player.StatsControl.StunDuration;
        _player.Anim.SetBool("StunState", true);
    }

    public override void Exit()
    {
        base.Exit();
        _player.Anim.SetBool("StunState", false);
        _player.StateControl.IsBusy = false;
    }

    public override void Update()
    {
        base.Update();
        if (!_player.IsOwner) return;
        // 击晕为不可打断的一次性硬直，时长到点回移动态
        if (_stateDuration <= 0f) _stateMachine.ChangeState(_player.StateControl.MovementState);
    }
}

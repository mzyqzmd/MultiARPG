using UnityEngine;

/// <summary>
/// 坠落动作态：独立于跳跃。玩家走出平台边缘(地面丢失)或跳跃到达顶点都会进入。
/// 垂直速度自然继承（边缘掉落来自地面下压，顶点来自跳跃末速），受重力持续下落，
/// 落地(HasLanded)回 MovementState。垂直运动经 PlayerVerticalMotionControl 统一计算。
/// </summary>
public class PlayerFallState : PlayerActionState
{
    private readonly PlayerVerticalMotionControl _verticalMotionControl;

    public PlayerFallState(StateMachine<PlayerActionState> stateMachine_, Player player_,
        CharacterController characterController_, PlayerVerticalMotionControl verticalMotionControl_) :
        base(stateMachine_, player_, characterController_)
    {
        _verticalMotionControl = verticalMotionControl_;
    }

    public override void Enter()
    {
        base.Enter();
        _player.StateControl.IsBusy = true;
        _player.Anim.SetBool("FallState", true);
    }

    public override void Exit()
    {
        base.Exit();
        _player.StateControl.IsBusy = false;
        _player.Anim.SetBool("FallState", false);
    }

    public override void Update()
    {
        base.Update();
        if (!_player.IsOwner) return;
        _verticalMotionControl.TickGravity(Time.deltaTime);

        Vector2 input = _player.InputService.MoveInputVector2;
        if (input != Vector2.zero)
        {
            Vector3 moveDir = GetMoveDirection(input);
            Move(moveDir * _player.StatsControl.NormalMoveSpeed + _verticalMotionControl.VerticalVelocity);
            RotateTowards(moveDir, _player.StateControl.IsInCombat);
        }
        else
        {
            Move(_verticalMotionControl.VerticalVelocity);
        }

        if (_verticalMotionControl.HasLanded())
        {
            _verticalMotionControl.Reset();
            _stateMachine.ChangeState(_player.StateControl.MovementState);
        }
    }
}

using UnityEngine;

/// <summary>
/// 坠落动作态：独立于跳跃。玩家走出平台边缘(地面丢失)或跳跃到达顶点都会进入。
/// 垂直速度自然继承（边缘掉落来自地面下压，顶点来自跳跃末速），受重力持续下落，
/// 落地(HasLanded)回 MovementState。垂直运动经 PlayerVerticalMotionControl 统一计算。
/// </summary>
public class PlayerFallState : PlayerActionState
{
    private readonly PlayerVerticalMotionControl _jumpControl;

    public PlayerFallState(StateMachine<PlayerActionState> stateMachine_,
        PlayerStateControl playerState_, CharacterController characterController_,
        PlayerStatsControl playerStats_, Animator anim_, InputService inputService_,
        PlayerCameraService playerCameraService_, AudioService audioService_,
        PlayerVerticalMotionControl jumpControl_) :
        base(stateMachine_, playerState_, characterController_, playerStats_,
            anim_, inputService_, playerCameraService_, audioService_)
    {
        _jumpControl = jumpControl_;
    }

    public override void Enter()
    {
        _playerStateControl.IsFalling = true;
        _anim.SetBool("FallState", true);
    }

    public override void Exit()
    {
        _playerStateControl.IsFalling = false;
        _anim.SetBool("FallState", false);
    }

    public override void Update()
    {
        base.Update();
        _jumpControl.TickGravity(Time.deltaTime);

        Vector2 input = _inputService.MoveInputVector2;
        if (input != Vector2.zero)
        {
            Vector3 moveDir = GetMoveDirection(input);
            Move(moveDir * _statsControl.NormalMoveSpeed + _jumpControl.VerticalVelocity);
            RotateTowards(moveDir, _playerStateControl.IsInCombat);
        }
        else
        {
            Move(_jumpControl.VerticalVelocity);
        }

        if (_jumpControl.HasLanded())
        {
            _jumpControl.Reset();
            _stateMachine.ChangeState(_playerStateControl.MovementState);
        }
    }
}

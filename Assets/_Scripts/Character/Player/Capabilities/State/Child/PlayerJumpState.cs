using UnityEngine;

/// <summary>
/// 跳跃动作态：由动作层 AnyTransition 抢断进入，赋初速度后受重力上升再下落。
/// 到达顶点(_verticalVelocity.y<=0 且仍空中)平滑转入 FallState（自然继承垂直速度）；
/// 落地(HasLanded)回 MovementState。垂直运动经 PlayerVerticalMotionControl 统一计算。
/// </summary>
public class PlayerJumpState : PlayerActionState
{
    private readonly PlayerVerticalMotionControl _verticalMotionControl;
    private const string JumpLiftAnimSpeedParam = "JumpLiftAnimSpeed";
    private float _jumpLiftClipLength;
    private bool _animSpeedReady;

    public PlayerJumpState(StateMachine<PlayerActionState> stateMachine_,
        PlayerStateControl stateControl_, CharacterController characterController_,
        PlayerStatsControl statsControl_, Animator anim_, InputService inputService_,
        PlayerCameraService playerCameraService_, AudioService audioService_,
        PlayerVerticalMotionControl verticalMotionControl_) :
        base(stateMachine_, stateControl_, characterController_, statsControl_,
            anim_, inputService_, playerCameraService_, audioService_)
    {
        _verticalMotionControl = verticalMotionControl_;
    }

    public override void Enter()
    {
        base.Enter();
        _verticalMotionControl.Reset();               // 清零残留垂直速度
        _verticalMotionControl.Launch();              // 赋向上初速度
        _stateControl.IsBusy = true;
        _stateControl.ConsumeJump();
        _statsControl.SpendStamina(_stateControl.JumpStaminaCost);
        _anim.SetBool("JumpState", true);
        // 已缓存 clip 长度则立即设倍速，避免首帧跳变
        _animSpeedReady = _jumpLiftClipLength > 0f;
        if (_animSpeedReady)
        {
            float timeToApex = _statsControl.GetJumpInitialVelocity() / -_statsControl.Gravity;
            _anim.SetFloat(JumpLiftAnimSpeedParam, _jumpLiftClipLength / timeToApex);
        }
    }

    public override void Exit()
    {
        _anim.SetBool("JumpState", false);
        _stateControl.IsBusy = false;
    }

    public override void Update()
    {
        base.Update();
        _verticalMotionControl.TickGravity(Time.deltaTime);

        // 首次进入 lift 后读取 clip 原始长度并缓存
        if (!_animSpeedReady)
        {
            var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Jump.Main_jump_lift"))   // 子状态机状态全名：SubStateMachine.State
            {
                _jumpLiftClipLength = stateInfo.length;
                float timeToApex = _statsControl.GetJumpInitialVelocity() / -_statsControl.Gravity;
                _anim.SetFloat(JumpLiftAnimSpeedParam, _jumpLiftClipLength / timeToApex);
                _animSpeedReady = true;
            }
        }

        #region 跳跃期间移动
        Vector2 input = _inputService.MoveInputVector2;
        if (input != Vector2.zero)
        {
            Vector3 moveDir = GetMoveDirection(input);
            Move(moveDir * _statsControl.NormalMoveSpeed + _verticalMotionControl.VerticalVelocity);
            RotateTowards(moveDir, _stateControl.IsInCombat);
        }
        else
        {
            Move(_verticalMotionControl.VerticalVelocity);
        }
        #endregion

        // 到达顶点转入坠落（自然继承当前垂直速度）
        if (_verticalMotionControl.VerticalVelocity.y <= 0f && !_verticalMotionControl.HasLanded())
        {
            _stateMachine.ChangeState(_stateControl.FallState);
            return;
        }

        // 落地回退移动态
        if (_verticalMotionControl.HasLanded())
        {
            _verticalMotionControl.Reset();
            _stateMachine.ChangeState(_stateControl.MovementState);
        }
    }
}

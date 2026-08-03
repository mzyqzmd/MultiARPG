using UnityEngine;

/// <summary>
/// 默认动作态：idle+move。每帧查询父层姿态决定 strafe(八向)或自由(前向+转向)，
/// 直接驱动角色移动与 Animator 混合参数。0 耐力时禁止跑步(防白嫖冲刺)。
/// 离地(边缘掉落)时转入 FallState；移动时经 VerticalMotion 下压贴地防抖。
/// </summary>
public class PlayerMovementState : PlayerActionState
{
    private readonly PlayerVerticalMotionControl _jumpControl;

    public PlayerMovementState(StateMachine<PlayerActionState> stateMachine_,
        PlayerStateControl stateControl_, CharacterController characterController_,
        PlayerStatsControl statsControl_, Animator anim_, InputService inputService_,
        PlayerCameraService playerCameraService_, AudioService audioService_,
        PlayerVerticalMotionControl jumpControl_) :
        base(stateMachine_, stateControl_, characterController_, statsControl_, anim_,
            inputService_, playerCameraService_, audioService_)
    {
        _jumpControl = jumpControl_;
    }

    public override void Enter()
    {
        base.Enter();
        _anim.SetBool("MovementState", true);
    }

    public override void Update()
    {
        base.Update();
        // 走出平台边缘(地面丢失)自动坠落
        if (!_jumpControl.CanJump)
        {
            _stateMachine.ChangeState(_stateControl.FallState);
            return;
        }
        bool combat = _stateControl.IsInCombat;
        // 0 耐力时即使按住跑步也降级为走路
        bool running = _inputService.IsRunning && _statsControl.CanExert;
        if (running)
            _statsControl.DrainStamina(_statsControl.RunStaminaDrain, Time.deltaTime);
        HandleMovement(combat, running);
        UpdateAnimator(combat, running);
    }

    public override void Exit()
    {
        base.Exit();
        _anim.SetBool("MovementState", false);
    }

    // 读输入驱动移动：战斗态 strafe 面向相机，自由态面向移动方向
    private void HandleMovement(bool strafe_, bool running_)
    {
        _jumpControl.SnapToGround();
        Vector2 input = _inputService.MoveInputVector2;
        if (input == Vector2.zero) return;
        Vector3 moveDir = GetMoveDirection(input);
        float speed = _statsControl.GetMoveSpeed(running_);
        Move(speed * moveDir + _jumpControl.VerticalVelocity);
        RotateTowards(moveDir, strafe_);
    }

    // 输入→Animator 混合参数：键盘未跑步时缩放到内环(走路)，战斗走八向、自由走前向
    private void UpdateAnimator(bool combat_, bool running_)
    {
        float scale = _inputService.ActiveDevice == EActiveDevice.Keyboard && !running_ ? 0.5f : 1f;
        Vector2 move = _inputService.MoveInputVector2 * scale;
        if (combat_)
        {
            _anim.SetFloat("RightInput", move.x, 0.1f, Time.deltaTime);
            _anim.SetFloat("ForwardInput", move.y, 0.1f, Time.deltaTime);
        }
        else
        {
            _anim.SetFloat("ForwardInput", move.magnitude, 0.1f, Time.deltaTime);
            _anim.SetFloat("RightInput", 0f, 0.1f, Time.deltaTime);
        }
        _anim.SetFloat("MoveVelocity", move.magnitude, 0.1f, Time.deltaTime);
        _anim.SetBool("IsInCombat", combat_);
    }
}

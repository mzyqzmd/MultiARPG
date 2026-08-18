using UnityEngine;

/// <summary>
/// 默认动作态：idle+move。每帧查询父层姿态决定 strafe(八向)或自由(前向+转向)，
/// 直接驱动角色移动与 Animator 混合参数。0 耐力时禁止跑步(防白嫖冲刺)。
/// 离地(边缘掉落)时转入 FallState；移动时经 VerticalMotion 下压贴地防抖。
/// </summary>
public class PlayerMovementState : PlayerActionState
{
    private readonly PlayerVerticalMotionControl _verticalMotionControl;

    public PlayerMovementState(StateMachine<PlayerActionState> stateMachine_, Player player_,
         CharacterController characterController_, PlayerVerticalMotionControl verticalMotionControl_) :
         base(stateMachine_, player_, characterController_)
    {
        _verticalMotionControl = verticalMotionControl_;
    }

    public override void Enter()
    {
        base.Enter();
        _player.Anim.SetBool("MovementState", true);
    }

    public override void Update()
    {
        base.Update();
        if (!_player.IsOwner) return;
        // 走出平台边缘(地面丢失)自动坠落
        if (!_verticalMotionControl.CanJump)
        {
            _stateMachine.ChangeState(_player.StateControl.FallState);
            return;
        }
        bool combat = _player.StateControl.IsInCombat;
        // 0 耐力时即使按住跑步也降级为走路
        bool running = _player.InputService.IsRunning && _player.StatsControl.CanExert;
        if (running)
            _player.StatsControl.DrainStamina(_player.StatsControl.RunStaminaDrain, Time.deltaTime);
        HandleMovement(combat, running);
        UpdateAnimator(combat, running);
    }

    public override void Exit()
    {
        base.Exit();
        _player.Anim.SetBool("MovementState", false);
    }

    // 读输入驱动移动：战斗态 strafe 面向相机，自由态面向移动方向
    private void HandleMovement(bool strafe_, bool running_)
    {
        _verticalMotionControl.SnapToGround();
        Vector2 input = _player.InputService.MoveInputVector2;
        if (input == Vector2.zero) return;
        Vector3 moveDir = GetMoveDirection(input);
        float speed = _player.StatsControl.GetMoveSpeed(running_);
        Move(speed * moveDir + _verticalMotionControl.VerticalVelocity);
        RotateTowards(moveDir, strafe_);
    }

    // 输入→Animator 混合参数：键盘未跑步时缩放到内环(走路)，战斗走八向、自由走前向
    private void UpdateAnimator(bool combat_, bool running_)
    {
        float scale = _player.InputService.ActiveDevice == EActiveDevice.Keyboard && !running_ ? 0.5f : 1f;
        Vector2 move = _player.InputService.MoveInputVector2 * scale;
        if (combat_)
        {
            // 锁敌：以"玩家→目标"为前向基准，将输入世界方向投影到目标坐标系，得到连续动画分量（非硬取反）
            var lockTarget = _player.PlayerCameraService.CurrentLockTarget;
            Vector3 forward = (lockTarget.LockCenter.position - _characterController.transform.position);
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 inputWorld = _player.PlayerCameraService.MoveForward * move.y +
                _player.PlayerCameraService.MoveRight * move.x;
            _player.Anim.SetFloat("ForwardInput", Vector3.Dot(inputWorld, forward), 0.1f, Time.deltaTime);
            _player.Anim.SetFloat("RightInput", Vector3.Dot(inputWorld, right), 0.1f, Time.deltaTime);
        }
        else
        {
            _player.Anim.SetFloat("ForwardInput", move.magnitude, 0.1f, Time.deltaTime);
            _player.Anim.SetFloat("RightInput", 0f, 0.1f, Time.deltaTime);
        }
        _player.Anim.SetFloat("MoveVelocity", move.magnitude, 0.1f, Time.deltaTime);
        _player.Anim.SetBool("IsInCombat", combat_);
    }
}

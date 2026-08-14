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

    public PlayerJumpState(StateMachine<PlayerActionState> stateMachine_, Player player_,
         CharacterController characterController_, PlayerVerticalMotionControl verticalMotionControl_) :
         base(stateMachine_, player_, characterController_)
    {
        _verticalMotionControl = verticalMotionControl_;
    }
    public override void Enter()
    {
        base.Enter();
        // 进入即破锁：触发事件经 Player 桥接 ForceUnlock 解除锁定 + FreeLook 过渡相机
        _player.StateControl.RequestLockBreak();
        _verticalMotionControl.Reset();               // 清零残留垂直速度
        _verticalMotionControl.Launch();              // 赋向上初速度
        _player.StateControl.IsBusy = true;
        _player.StateControl.ConsumeJump();
        _player.StatsControl.SpendStamina(_player.StateControl.JumpStaminaCost);
        _player.Anim.SetBool("JumpState", true);
        // 已缓存 clip 长度则立即设倍速，避免首帧跳变
        _animSpeedReady = _jumpLiftClipLength > 0f;
        if (_animSpeedReady)
        {
            float timeToApex = _player.StatsControl.GetJumpInitialVelocity() / -_player.StatsControl.Gravity;
            _player.Anim.SetFloat(JumpLiftAnimSpeedParam, _jumpLiftClipLength / timeToApex);
        }
    }

    public override void Exit()
    {
        base.Exit();
        _player.Anim.SetBool("JumpState", false);
        _player.StateControl.IsBusy = false;
    }

    public override void Update()
    {
        base.Update();
        if (!_player.IsOwner) return;
        _verticalMotionControl.TickGravity(Time.deltaTime);

        // 首次进入 lift 后读取 clip 原始长度并缓存
        if (!_animSpeedReady)
        {
            var stateInfo = _player.Anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Jump.Main_jump_lift"))   // 子状态机状态全名：SubStateMachine.State
            {
                _jumpLiftClipLength = stateInfo.length;
                float timeToApex = _player.StatsControl.GetJumpInitialVelocity() / -_player.StatsControl.Gravity;
                _player.Anim.SetFloat(JumpLiftAnimSpeedParam, _jumpLiftClipLength / timeToApex);
                _animSpeedReady = true;
            }
        }

        #region 跳跃期间移动
        Vector2 input = _player.InputService.MoveInputVector2;
        Vector3 moveDir;
        if (input != Vector2.zero)
        {
            moveDir = GetMoveDirection(input);
            Move(moveDir * _player.StatsControl.GetMoveSpeed(false) +
                _verticalMotionControl.VerticalVelocity);
        }
        else
        {
            // 无输入：朝相机前向水平位移，保持原垂直速度
            moveDir = _player.PlayerCameraService.MoveForward;
            Move(_verticalMotionControl.VerticalVelocity);
        }
        RotateTowards(moveDir, _player.StateControl.IsInCombat);
        #endregion

        // 到达顶点转入坠落（自然继承当前垂直速度）
        if (_verticalMotionControl.VerticalVelocity.y <= 0f && !_verticalMotionControl.HasLanded())
        {
            _stateMachine.ChangeState(_player.StateControl.FallState);
            return;
        }

        // 落地回退移动态
        if (_verticalMotionControl.HasLanded())
        {
            _verticalMotionControl.Reset();
            _stateMachine.ChangeState(_player.StateControl.MovementState);
        }
    }
}

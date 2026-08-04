using UnityEngine;

/// <summary>
/// 闪避动作态：覆盖常规移动的一段爆发位移，结束后回到 Locomotion。
/// 由动作层 High 优先级 AnyTransition 抢断进入，自由/战斗姿态下均可用。
/// 持续时间复用基类 _stateDuration(取自 PlayerStatsControl.DodgeDuration)。
/// </summary>
public class PlayerDodgeState : PlayerActionState
{
    private const string DodgeAnimSpeedParam = "DodgeAnimSpeed";

    private Vector3 _dodgeVelocity;
    private bool _animSpeedReady;
    private float _dodgeClipLength;

    public PlayerDodgeState(StateMachine<PlayerActionState> stateMachine_, 
        PlayerStateControl stateControl_, CharacterController characterController_,
        PlayerStatsControl statsControl_, Animator anim_, InputService inputService_, 
        PlayerCameraService playerCameraService_, AudioService audioService_, 
        UIService uiService_, VFXService vfxService_) : base(stateMachine_,
            stateControl_, characterController_, statsControl_, anim_, 
            inputService_, playerCameraService_, audioService_, uiService_, vfxService_)
    {
    }

    public override void Enter()
    {
        // 有输入则朝输入方向闪避，否则朝当前面向
        Vector3 dir = GetMoveDirection(_inputService.MoveInputVector2);
        if (dir == Vector3.zero) dir = _characterController.transform.forward;
        _dodgeVelocity = dir * _statsControl.DodgeSpeed;
        _stateDuration = _statsControl.DodgeDuration;

        _stateControl.IsBusy = true;
        _stateControl.ConsumeDodge();
        _statsControl.SpendStamina(_stateControl.DodgeStaminaCost);

        _audioService.PlayAudio("Player", "DodgeSFX", _characterController.transform.position);

        _anim.SetBool("DodgeState", true);
        // 已缓存 clip 长度则立即设定倍速，避免首帧用默认倍速 1 的跳变
        _animSpeedReady = _dodgeClipLength > 0f;
        if (_animSpeedReady)
            _anim.SetFloat(DodgeAnimSpeedParam, _dodgeClipLength / _statsControl.DodgeDuration);
    }

    public override void Exit() => _stateControl.IsBusy = false;

    public override void Update()
    {
        base.Update();
        // Dodge 动画 clip 长度与配置的 DodgeDuration 可能不一致：首次需待 Animator 真正切入 Dodge 后
        // 读取 clip 原始长度并缓存，之后按 clipLength/DodgeDuration 设定 Speed Multiplier，使动画恰好在
        // DodgeDuration 内播完；后续进入直接用缓存长度在 Enter 即时设定，无首帧跳变。
        if (!_animSpeedReady)
        {
            var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Dodge"))
            {
                _dodgeClipLength = stateInfo.length;
                _anim.SetFloat(DodgeAnimSpeedParam, _dodgeClipLength / _statsControl.DodgeDuration);
                _animSpeedReady = true;
            }
        }
        Move(_dodgeVelocity);
        if (_stateDuration <= 0f)
        {
            _anim.SetBool("DodgeState", false);
            _stateMachine.ChangeState(_stateControl.MovementState);
        }
    }
}

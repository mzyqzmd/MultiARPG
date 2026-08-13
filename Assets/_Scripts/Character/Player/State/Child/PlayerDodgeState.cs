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
        Player player_, CharacterController characterController_) :
        base(stateMachine_, player_, characterController_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        // 进入即破锁：触发事件经 Player 桥接 ForceUnlock 解除锁定 + FreeLook 过渡相机
        _player.StateControl.RequestLockBreak();

        //位移
        // 有移动输入则朝输入方向闪避，否则朝相机前向
        Vector3 dir = GetMoveDirection(_player.InputService.MoveInputVector2);
        if (dir == Vector3.zero) dir = _player.PlayerCameraService.MoveForward;
        // 翻滚无八向动画（固定向前翻滚），进入瞬间把玩家朝向硬切到 dir，使翻滚朝正确方向
        if (dir != Vector3.zero)
            _characterController.transform.rotation = Quaternion.LookRotation(dir);
        _dodgeVelocity = dir * _player.StatsControl.DodgeSpeed;
        _stateDuration = _player.StatsControl.DodgeDuration;

        _player.StateControl.IsBusy = true;
        _player.StateControl.ConsumeDodge();
        _player.StatsControl.SpendStamina(_player.StateControl.DodgeStaminaCost);

        // 音效
        _player.AudioService.PlayAudio("Player", "DodgeSFX", _characterController.transform.position);

        //动画
        _player.Anim.SetBool("DodgeState", true);
        // 已缓存 clip 长度则立即设定倍速，避免首帧用默认倍速 1 的跳变
        _animSpeedReady = _dodgeClipLength > 0f;
        if (_animSpeedReady)
            _player.Anim.SetFloat(DodgeAnimSpeedParam, _dodgeClipLength / _player.StatsControl.DodgeDuration);
    }

    public override void Exit()
    {
        base.Exit();
        _player.Anim.SetBool("DodgeState", false);
        _player.StateControl.IsBusy = false;
    }

    public override void Update()
    {
        base.Update();
        if (!_player.IsOwner) return;
        // Dodge 动画 clip 长度与配置的 DodgeDuration 可能不一致：首次需待 Animator 真正切入 Dodge 后
        // 读取 clip 原始长度并缓存，之后按 clipLength/DodgeDuration 设定 Speed Multiplier，使动画恰好在
        // DodgeDuration 内播完；后续进入直接用缓存长度在 Enter 即时设定，无首帧跳变。
        if (!_animSpeedReady)
        {
            var stateInfo = _player.Anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Dodge"))
            {
                _dodgeClipLength = stateInfo.length;
                _player.Anim.SetFloat(DodgeAnimSpeedParam, _dodgeClipLength / _player.StatsControl.DodgeDuration);
                _animSpeedReady = true;
            }
        }
        Move(_dodgeVelocity);
        if (_stateDuration <= 0f)
        {
            _player.Anim.SetBool("DodgeState", false);
            _stateMachine.ChangeState(_player.StateControl.MovementState);
        }
    }
}

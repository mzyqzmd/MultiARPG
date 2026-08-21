using UnityEngine;

/// <summary>
/// 轻击动作态：进入置 NormalAttackState bool 触发控制器进入轻击子状态机，播第一段（Normal_01）。
/// 连段（Normal_01→Normal_02→Normal_03...）由 AC_Player 控制器状态机 + 每段 clip 的动画事件（OnComboWindowOpen/Close）驱动：
/// 窗口仅在 OnComboWindowOpen→Close 期间开启（StateControl.ComboWindowOpen），AnyTransition 才允许重入进下一段；
/// 窗口外（前摇/后摇）的轻击请求被 StateControl.Update 丢弃，不计入连段。C# 不硬编码段数，加段仅在控制器加状态+连线。
/// 动画经 AnimatorController 状态机驱动，由 ClientNetworkAnimator（owner 权威）原生跨端同步。
/// </summary>
public class PlayerNormalAttackState : PlayerActionState
{
    private const string NormalAttackAnimSpeedParam = "NormalAttackSpeed";
    private const string NormalAttackStateTag = "NormalAttack";
    private const string NormalComboTrigger = "NormalCombo";

    public PlayerNormalAttackState(StateMachine<PlayerActionState> stateMachine_,
        Player player_, CharacterController characterController_) :
        base(stateMachine_, player_, characterController_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        _player.StateControl.IsBusy = true;
        _player.StateControl.ConsumeNormalAttack();
        _player.StatsControl.SpendStamina(_player.StateControl.NormalAttackStaminaCost);
        _player.Anim.SetBool("NormalAttackState", true);
        _stateDuration = _player.StatsControl.NormalAttackDuration;
    }

    public override void Update()
    {
        base.Update();
        if (!_player.IsOwner) return;
        var stateInfo = _player.Anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsTag(NormalAttackStateTag))
            _player.Anim.SetFloat(NormalAttackAnimSpeedParam, stateInfo.length / _player.StatsControl.NormalAttackDuration);
        // 连段：窗口期内检测到轻击请求 → 置 NormalCombo trigger 驱动 Animator 内部过渡 01→02；消费请求避免重复触发
        if (_player.StateControl.NormalAttackRequested && _player.StateControl.ComboWindowOpen)
        {
            _player.StateControl.ConsumeNormalAttack();
            _player.Anim.SetTrigger(NormalComboTrigger);
        }
        // 超时兜底：动画事件 ReturnToMovementState 可能因 clip 未播到末帧而丢失(连段狂按重入/退出过渡卡末帧)，
        // 导致 Exit 不执行、IsBusy 永久锁死。计时到点且尚未返回时强切 MovementState，与动画事件形成双保险。
        if (_stateDuration <= 0f)
            _player.StateControl.ReturnToMovementState();
    }

    public override void Exit()
    {
        base.Exit();
        _player.Anim.ResetTrigger(NormalComboTrigger);
        _player.Anim.SetBool("NormalAttackState", false);
        _player.StateControl.ConsumeNormalAttack();
        _player.StateControl.IsBusy = false;
    }
}

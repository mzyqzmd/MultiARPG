using Animancer;
using UnityEngine;

/// <summary>
/// 轻击动作态：按当前武器 SO_Weapon.WeaponAnimations 字典动态播放连击段（Normal_0/1/2...），
/// 段数由字典中存在的 key 数量决定，无需为每把武器新建状态类。
/// 连击衔接由每段 clip 末尾动画事件（OnComboWindowOpen / OnAttackEnd）驱动：
/// 窗口内再次收到轻击请求且存在下一段则播下一段，否则退出回移动态。
/// 动画经 HybridAnimancerComponent 播放覆盖控制器，播完淡回 Locomotion（由 MovementState 接管）。
/// </summary>
public class PlayerNormalAttackState : PlayerActionState
{
    private int _comboIndex;
    private SO_Weapon _weapon;
    private AnimancerState _currentState;
    private bool _comboWindowOpen;

    public PlayerNormalAttackState(StateMachine<PlayerActionState> stateMachine_,
        Player player_, CharacterController characterController_) :
        base(stateMachine_, player_, characterController_)
    {
    }

    public override void Enter()
    {
        _comboIndex = 0;
        _comboWindowOpen = false;
        _weapon = _player.EquipmentControl.GetEquippedItem(EEquipmentSlot.RightHand)?.ItemData as SO_Weapon;
        _player.StateControl.IsBusy = true;
        _player.StateControl.ConsumeNormalAttack();
        _player.StatsControl.SpendStamina(_player.StateControl.NormalAttackStaminaCost);
        PlaySegment();
    }

    public override void Exit()
    {
        _player.StateControl.ConsumeNormalAttack();
        _player.StateControl.IsBusy = false;
        _comboWindowOpen = false;
    }

    public override void Update()
    {
        base.Update();
        // 窗口内提前连段：收到新轻击请求且存在下一段则立即播下一段（不等 clip 自然结束）
        if (_comboWindowOpen && _player.StateControl.NormalAttackRequested && HasNextSegment())
        {
            _comboIndex++;
            _player.StateControl.ConsumeNormalAttack();
            PlaySegment();
        }
    }

    /// <summary>动画事件 OnComboWindowOpen：开启可连段窗口。</summary>
    public void OpenComboWindow() => _comboWindowOpen = true;

    /// <summary>动画事件 OnAttackEnd：关闭窗口，由 OnClipEnd 决定退出与否。</summary>
    public void CloseComboWindow() => _comboWindowOpen = false;

    private bool HasNextSegment()
    {
        if (_weapon == null) return false;
        var key = $"Normal_{_comboIndex + 1}";
        return _weapon.WeaponAnimations.ContainsKey(key);
    }

    private void PlaySegment()
    {
        if (_weapon == null || !TryGetClip($"Normal_{_comboIndex}", out var clip))
        {
            ChangeToMovement();
            return;
        }
        _comboWindowOpen = false;
        _currentState = _player.Animancer.Play(clip);
        if (_currentState.Events(this, out var events))
            events.OnEnd = OnClipEnd;
    }

    private void OnClipEnd()
    {
        // clip 自然结束：窗口内收到新请求且存在下一段则连段，否则退出回移动态
        if (_player.StateControl.NormalAttackRequested && HasNextSegment())
        {
            _comboIndex++;
            _player.StateControl.ConsumeNormalAttack();
            PlaySegment();
        }
        else
        {
            ChangeToMovement();
        }
    }

    private void ChangeToMovement()
    {
        // Hybrid 模式：用 PlayController 让控制器恢复输出（取代 Stop——Stop 会令 Hybrid 失去输出导致角色停在原 pose），
        // 带 FadeDuration 平滑淡回 Locomotion，避免硬切错位。
        _player.Animancer.PlayController();
        _stateMachine.ChangeState(_player.StateControl.MovementState);
    }

    private bool TryGetClip(string key_, out ClipTransition clip_)
    {
        clip_ = default;
        return _weapon != null && _weapon.WeaponAnimations.TryGetValue(key_, out clip_);
    }
}

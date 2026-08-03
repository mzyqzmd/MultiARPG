using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 轻击动作态：播放一次挥砍动画（倍速由配置时长驱动），期间武器碰撞体自治命中。
/// 由动作层 High 优先级 AnyTransition 抢断进入，自由/战斗姿态下均可用（IsInCombat 仅管移动动画，不门禁攻击）。
/// 持续时间复用基类 _stateDuration(取自 PlayerStatsControl.NormalAttackDuration)。
/// 动画倍速每帧从当前实际播放的 AnimationClip 原始时长计算(非缓存)，故连击各段 clip 不同时各自缩放正确；
/// 识别用 GetCurrentAnimatorClipInfo 取真实 motion，规避子状态机包裹下 IsTag 不可靠的问题，且无需按状态名硬编码。
/// </summary>
public class PlayerNormalAttackState : PlayerActionState
{
    private const string NormalAttackAnimSpeedParam = "NormalAttackSpeed";
    private const string NormalAttackStateTag = "NormalAttack";

    public PlayerNormalAttackState(StateMachine<PlayerActionState> stateMachine_,
        PlayerStateControl stateControl_, CharacterController characterController_,
        PlayerStatsControl statsControl_, Animator anim_, InputService inputService_,
        PlayerCameraService playerCameraService_, AudioService audioService_) :
        base(stateMachine_, stateControl_, characterController_, statsControl_,
            anim_, inputService_, playerCameraService_, audioService_)
    {
    }

    public override void Enter()
    {
        _stateDuration = _statsControl.NormalAttackDuration;

        _stateControl.IsBusy = true;
        _stateControl.ConsumeNormalAttack();
        _anim.SetBool("NormalAttackState", true);
        _statsControl.SpendStamina(_stateControl.NormalAttackStaminaCost);

        //TODO:给NormalAttack添加音效
        //_audioService.PlayAudio("Player", "NormalAttackSFX", _characterController.gameObject.transform);
    }

    public override void Exit()
    {
        _stateControl.ConsumeNormalAttack();
        _stateControl.IsBusy = false;
    }

    public override void Update()
    {
        base.Update();
        var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsTag(NormalAttackStateTag))
            _anim.SetFloat(NormalAttackAnimSpeedParam, stateInfo.length / _statsControl.NormalAttackDuration);
        if (_stateDuration <= 0f)
        {
            // 先清掉轻击请求再切回移动：阻断本帧 EvaluateTransitions 用残留/新置的请求立即重抢断攻击态
            _stateControl.ConsumeNormalAttack();
            _anim.SetBool("NormalAttackState", false);
            _stateMachine.ChangeState(_stateControl.MovementState);
        }
    }
}

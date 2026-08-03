using UnityEngine;


/// <summary>
/// 自由姿态：前向+转向移动，使用普通移动动画(1D 风格)。
/// </summary>
public class PlayerFreeState : PlayerStanceState
{

    public PlayerFreeState(StateMachine<PlayerStanceState> stateMachine_,
        PlayerStateControl stateControl_, Animator anim_) : base(stateMachine_, stateControl_, anim_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        _stateControl.CombatLocked = false;
        _anim.SetBool("IsInCombat", false);
    }
}

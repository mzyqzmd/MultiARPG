using UnityEngine;


/// <summary>
/// 战斗姿态：八向strafe移动，使用战斗动画(2D 混合树)，允许攻击/格挡。
/// </summary>
public class PlayerCombatState : PlayerStanceState
{
    public PlayerCombatState(StateMachine<PlayerStanceState> stateMachine_,
        PlayerStateControl stateControl_, Animator anim_) : base(stateMachine_, stateControl_, anim_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        _stateControl.CombatLocked = true;
        _anim.SetBool("IsInCombat", true);
    }
}

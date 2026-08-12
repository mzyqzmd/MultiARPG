using UnityEngine;


/// <summary>
/// 自由姿态：前向+转向移动，使用普通移动动画(1D 风格)。
/// </summary>
public class PlayerFreeState : PlayerStanceState
{
    public PlayerFreeState(StateMachine<PlayerStanceState> stateMachine_,
        Player player_) : base(stateMachine_, player_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        _player.StateControl.CombatLocked = false;
        _player.Anim.SetBool("IsInCombat", false);
    }
}

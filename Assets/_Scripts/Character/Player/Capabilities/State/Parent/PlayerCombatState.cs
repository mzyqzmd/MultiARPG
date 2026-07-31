/// <summary>
/// 战斗姿态：八向strafe移动，使用战斗动画(2D 混合树)，允许攻击/格挡。
/// </summary>
public class PlayerCombatState : PlayerStanceState
{
    public override bool IsInCombat => true;

    public PlayerCombatState(StateMachine<PlayerStanceState> stateMachine_,
        PlayerStateControl playerState_) : base(stateMachine_, playerState_)
    {
    }
}

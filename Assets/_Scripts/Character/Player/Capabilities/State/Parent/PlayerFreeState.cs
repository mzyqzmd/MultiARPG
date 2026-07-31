/// <summary>
/// 自由姿态：前向+转向移动，使用普通移动动画(1D 风格)。
/// </summary>
public class PlayerFreeState : PlayerStanceState
{
    public override bool IsInCombat => false;

    public PlayerFreeState(StateMachine<PlayerStanceState> stateMachine_,
        PlayerStateControl playerState_) : base(stateMachine_, playerState_)
    {
    }
}

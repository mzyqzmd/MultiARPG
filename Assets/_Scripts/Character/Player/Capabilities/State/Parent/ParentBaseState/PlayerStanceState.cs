/// <summary>
/// 父层(姿态层)状态基类：决定玩家移动与动画风格。
/// 自由姿态=前向+转向(1D)，战斗姿态=八向strafe(2D)。
/// 移动方式由本层各姿态经 IsInCombat 显式声明，动作层据此选择(不再由 Control 持有策略)。
/// </summary>
public abstract class PlayerStanceState : PlayerBaseState<PlayerStanceState>
{
    /// <summary>当前姿态是否为战斗(决定动作层 strafe/自由移动方式)。</summary>
    public abstract bool IsInCombat { get; }

    protected PlayerStanceState(StateMachine<PlayerStanceState> stateMachine_,
        PlayerStateControl playerState_) : base(stateMachine_, playerState_)
    {
    }
}

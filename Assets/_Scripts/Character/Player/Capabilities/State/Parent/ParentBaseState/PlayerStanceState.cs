using UnityEngine;


/// <summary>
/// 父层(姿态层)状态基类：决定玩家移动与动画风格。
/// 自由姿态=前向+转向(1D)，战斗姿态=八向strafe(2D)。
/// 移动方式由本层各姿态经 IsInCombat 显式声明，动作层据此选择(不再由 Control 持有策略)。
/// </summary>
public abstract class PlayerStanceState : PlayerBaseState<PlayerStanceState>
{
    protected readonly Animator _anim;

    protected PlayerStanceState(StateMachine<PlayerStanceState> stateMachine_,
        PlayerStateControl stateControl_, Animator anim_) : base(stateMachine_, stateControl_)
    {
        _anim = anim_;
    }
}

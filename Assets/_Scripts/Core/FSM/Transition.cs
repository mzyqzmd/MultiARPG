using System;
/// <summary>
/// 条件转换规则：from状态满足condition时自动切换到to状态。
/// From为default(TState)时表示从任意状态触发。
/// </summary>
public struct Transition<TState> where TState : IState
{
    public TState from;
    public TState to;
    public Func<bool> condition;
    public ETransitionPriority priority;
}
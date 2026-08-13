using UnityEngine;


/// <summary>
/// 玩家状态基类(泛型)：通过 TState 区分所属层级。
/// 姿态层用 PlayerStanceState、动作层用 PlayerActionState，二者共用本基类与 IState 生命周期。
/// 依赖以构造函数注入：状态机引用 + 状态编排器(PlayerState)。
/// </summary>
public abstract class PlayerBaseState<TState> : IState where TState : IState
{
    protected readonly StateMachine<TState> _stateMachine;
    protected readonly Player _player;
    protected float _stateDuration;

    protected PlayerBaseState(StateMachine<TState> stateMachine_,Player player_)
    {
        _stateMachine = stateMachine_;
        _player = player_;
    }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void Update()
    {
        // 非 owner 角色不本地驱动状态机/动画参数：位置由 ClientNetworkTransform 同步、动画由 ClientNetworkAnimator 同步，
        // 本地读取全局输入写 Animator 会污染远端表现（串台）。
        if (!_player.IsOwner) return;
        if (_stateDuration > 0) _stateDuration -= Time.deltaTime;
    }
}

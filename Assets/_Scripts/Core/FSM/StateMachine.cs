using System;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// 不设子状态机时作为普通状态机使用；调用SetChildFsm后变为分层状态机，父层和子层同时Update。
/// </summary>
public class StateMachine<TState> where TState : IState
{
    // ==================== 本层 ====================

    /// <summary>当前状态</summary>
    public TState CurrentState { get; private set; }
    /// <summary>上一个状态</summary>
    public TState PreviousState { get; private set; }
    /// <summary>状态切换事件，参数为(切换前状态, 切换后状态)</summary>
    public UnityAction<TState, TState> OnStateChanged;

    private readonly List<Transition<TState>> _transitions = new();

    // ==================== 子层 ====================

    private object _childFsm;
    private UnityAction _childUpdate;

    #region 设置子状态机
    /// <summary>
    /// 设置子状态机，将当前状态机变为分层状态机。
    /// 返回子状态机引用，后续通过返回的引用操作子层的切换和条件转换。
    /// </summary>
    public StateMachine<TChild> SetChildFsm<TChild>() where TChild : IState
    {
        var child = new StateMachine<TChild>();
        _childFsm = child;
        _childUpdate += child.Update;
        return child;
    }
    #endregion

    #region 获取子状态机引用，需先调用SetChildFsm
    public StateMachine<TChild> GetChildFsm<TChild>() where TChild : IState =>
         _childFsm as StateMachine<TChild>;
    #endregion

    #region 初始化状态机
    public void Initialize(TState state_)
    {
        CurrentState = state_;
        CurrentState?.Enter();
    }
    #endregion

    #region 状态切换
    public void ChangeState(TState newState_)
    {
        TState from = CurrentState;
        CurrentState?.Exit();
        PreviousState = from;
        CurrentState = newState_;
        CurrentState?.Enter();
        OnStateChanged?.Invoke(from, newState_);
    }
    #endregion

    #region 添加指定状态转换条件
    /// <summary>
    /// 当前状态为from且condition满足时自动切换到to。
    /// priority默认为Normal，同优先级按注册顺序判断。
    /// </summary>
    public void AddTransition(TState from_, TState to_, Func<bool> condition_,
        ETransitionPriority priority_ = ETransitionPriority.Normal)
    {
        InsertSorted(new Transition<TState>
        {
            from = from_,
            to = to_,
            condition = condition_,
            priority = priority_
        });
    }
    #endregion

    #region 添加任意状态转换条件
    /// <summary>
    /// 无论当前是什么状态，condition满足时自动切换到to。
    /// 适合受伤、死亡等全局响应逻辑。
    /// priority默认为Normal，同优先级按注册顺序判断。
    /// </summary>
    public void AddAnyTransition(TState to_, Func<bool> condition_,
        ETransitionPriority priority_ = ETransitionPriority.Normal)
    {
        InsertSorted(new Transition<TState>
        {
            from = default,
            to = to_,
            condition = condition_,
            priority = priority_
        });
    }
    #endregion

    #region 按优先级降序插入，同优先级追加到末尾（保持注册顺序）。
    private void InsertSorted(Transition<TState> item_)
    {
        for (int i = 0; i < _transitions.Count; i++)
        {
            if (item_.priority > _transitions[i].priority)
            {
                _transitions.Insert(i, item_);
                return;
            }
        }
        _transitions.Add(item_);
    }
    #endregion

    #region Update驱动
    public void Update()
    {
        CurrentState?.Update();
        _childUpdate?.Invoke();
        EvaluateTransitions();
    }
    #endregion

    #region 按优先级从高到低、同优先级按注册顺序遍历转换规则
    private void EvaluateTransitions()
    {
        foreach (var t in _transitions)
        {
            //如果条件为空或条件不满足则跳过
            if (t.condition == null || !t.condition()) continue;
            //如果当前状态为指定状态则切换
            if (t.from == null || EqualityComparer<TState>.Default.Equals(t.from, CurrentState))
            {
                ChangeState(t.to);
                break;
            }
        }
    }
    #endregion
}

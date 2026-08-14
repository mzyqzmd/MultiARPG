using NodeCanvas.BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI NPC 抽象基类（继承 Character）：复用车角色数值/受击/锁定体系，新增寻路、行为树、视觉、动画桥接与激活管理。
/// 行为树/NavMesh/动画仅在 Host 端驱动；客户端经默认 NetworkAnimator(server 权威) 同步动画。
/// </summary>
public abstract class AINPC : Character
{
    [Header("AI 配置")]
    [SerializeField] protected NavMeshAgent _navAgent;
    [SerializeField] protected AINpcAnimatorBridge _animBridge;
    [SerializeField] protected BehaviourTreeOwner _btOwner;
    [SerializeField] protected AIVision _vision;
    [SerializeField] private float _activationRadius = 20f;

    public NavMeshAgent NavAgent => _navAgent;
    public AINpcAnimatorBridge AnimBridge => _animBridge;
    public AIVision Vision => _vision;
    public float ActivationRadius => _activationRadius;
    public Transform TargetPlayer { get; set; }

    private bool _active;

    #region 行为树语义入口（由 BTNodes 调用，子类可重写）
    /// <summary>执行一次攻击：默认空，由子类（如 NormalZombie）算方向后交 AnimBridge.TriggerAttack。</summary>
    public virtual void PerformAttack() { }

    /// <summary>死亡收尾：停行为树 + 标记死亡动画 + 失活 Agent。</summary>
    public virtual void Die()
    {
        if (IsServer && _btOwner.isRunning) _btOwner.StopBehaviour();
        _navAgent.ResetPath();
        _animBridge.SetDead();
    }
    #endregion

    #region 移动速度（子类按走/跑配置重写）
    /// <summary>切换行走/奔跑速度：基类空实现，子类按配置重写并设置 NavAgent.speed。</summary>
    public virtual void ApplyMoveSpeed(bool running_) { }
    #endregion

    #region 激活管理（AINpcService 调用）
    /// <summary>按玩家距离激活/失活：激活时启行为树与 Agent；失活时停树并回待机。</summary>
    public virtual void SetActive(bool active_)
    {
        if (active_ == _active) return;
        _active = active_;
        if (active_)
        {
            _navAgent.enabled = true;
            _animBridge.SetIdle();
            if (IsServer) _btOwner.StartBehaviour();
        }
        else
        {
            if (IsServer && _btOwner.isRunning) _btOwner.StopBehaviour();
            _navAgent.ResetPath();
            _animBridge.SetIdle();
        }
    }
    #endregion
}

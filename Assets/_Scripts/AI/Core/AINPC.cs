using NodeCanvas.BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI NPC 抽象基类（继承 Character）：复用车角色数值/受击/锁定体系，新增寻路、行为树、视觉、动画桥接与激活管理。
/// 行为树/NavMesh/动画仅在 Host 端驱动；客户端经默认 NetworkAnimator(server 权威) 同步动画。
/// </summary>
[RequireComponent(typeof(BehaviourTreeOwner))]
public abstract class AINPC : Character
{
    [Header("依赖配置")]
    [SerializeField] protected NavMeshAgent _navAgent;
    [SerializeField] protected BehaviourTreeOwner _btOwner;
    [Header("玩家靠近时激活范围")]
    [SerializeField] protected float _activationRadius = 20f;
    [Header("视觉参数（起点前后共用，遮挡层共用，角度前后互补）")]
    [SerializeField] protected Transform _lookStartPoint;
    [SerializeField] protected float _frontViewDistance = 12f;
    [SerializeField] protected float _backViewDistance = 6f;
    [SerializeField] protected float _viewAngle = 110f;
    [SerializeField] protected LayerMask _obstacleMask;
    [Header("残留记忆：丢失视野后仍视为可见的秒数")]
    [SerializeField] protected float _memoryDuration = 3f;

    public NavMeshAgent NavAgent => _navAgent;

    public float ActivationRadius => _activationRadius;
    public Transform TargetPlayer { get; set; }

    protected AINpcService _aiNPCService;

    protected bool _active;
    private float _lastSeenTime = -Mathf.Infinity;

    protected override void Awake()
    {
        base.Awake();
        _aiNPCService = Services.Get<AINpcService>();
    }

    #region 生命周期（向 AINpcService 自注册/反注册）
    /// <summary>网络生成时向调度服务自报存在；失活态初始由服务统一判定。</summary>
    public override void OnNetworkSpawn()
    {
        _aiNPCService.RegisterNpc(this);
    }

    /// <summary>网络销毁时从调度服务移除，避免注册表残留。</summary>
    public override void OnNetworkDespawn()
    {
        _aiNPCService.UnregisterNpc(this);
    }
    #endregion

    #region 视觉判定（前后向合并 + 残留记忆）
    /// <summary>目标是否可见（前或后任一实时可见，或 3 秒内曾见过）。</summary>
    public bool CanSeeTarget(Transform target_)
    {
        if (target_ == null) return false;
        bool seenNow = CheckVisible(target_, _lookStartPoint.forward, _frontViewDistance, _viewAngle)
                    || CheckVisible(target_, -_lookStartPoint.forward, _backViewDistance, 360f - _viewAngle);
        if (seenNow) _lastSeenTime = Time.time;
        return seenNow || (Time.time - _lastSeenTime <= _memoryDuration);
    }
    /// <summary>目标是否在指定方向扇区内且无遮挡。</summary>
    private bool CheckVisible(Transform target_, Vector3 dir_, float dist_, float angle_)
    {
        var toTarget = target_.position - _lookStartPoint.position;
        float dist = toTarget.magnitude;
        if (dist > dist_) return false;

        var dir = toTarget / dist;
        float angle = Vector3.Angle(dir_, dir);
        if (angle > angle_ * 0.5f) return false;

        if (_obstacleMask != 0 &&
            Physics.Linecast(_lookStartPoint.position, target_.position, _obstacleMask))
            return false;

        return true;
    }
    #endregion
    /// <summary>死亡：行为树语义入口</summary>
    public abstract void Die();

    /// <summary>按玩家距离激活/失活：激活管理（AINpcService 调用）</summary>
    public abstract void SetActive(bool active_);

    #region 视线范围可视化
    /// <summary>编辑器 Gizmo：绘制前（绿）/后（蓝）双视锥与玩家激活范围球（经 GizmoDrawer 静态工具）。</summary>
    protected virtual void OnDrawGizmos()
    {
        Transform origin = _lookStartPoint != null ? _lookStartPoint : transform;
        GizmoDrawer.DrawVisionCone(origin.position, origin.forward, _frontViewDistance, _viewAngle * 0.5f, Color.green);
        GizmoDrawer.DrawVisionCone(origin.position, -origin.forward, _backViewDistance, (360f - _viewAngle) * 0.5f, Color.red);

        // 玩家激活范围（仅描线边框）
        GizmoDrawer.DrawWireSphere(transform.position, _activationRadius, new Color(1f, 1f, 0f, 0.4f));
    }
    #endregion
}

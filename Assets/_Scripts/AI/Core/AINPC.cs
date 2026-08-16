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
    [Header("视觉参数")]
    [SerializeField] protected Transform _lookStartPoint;
    [SerializeField] protected float _viewDistance = 12f;
    [SerializeField] protected float _viewAngle = 110f;
    [SerializeField] protected LayerMask _obstacleMask;

    public NavMeshAgent NavAgent => _navAgent;

    public float ActivationRadius => _activationRadius;
    public Transform TargetPlayer { get; set; }

    protected AINpcService _aiNPCService;

    protected bool _active;

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

    public bool CanSeeTarget(Transform target_)
    {
        if (target_ == null) return false;
        var toTarget = target_.position - _lookStartPoint.position;
        float dist = toTarget.magnitude;
        if (dist > _viewDistance) return false;

        var dir = toTarget / dist;
        float angle = Vector3.Angle(_lookStartPoint.forward, dir);
        if (angle > _viewAngle * 0.5f) return false;

        if (_obstacleMask != 0 &&
            Physics.Linecast(_lookStartPoint.position, target_.position, _obstacleMask))
            return false;

        return true;
    }
    /// <summary>死亡：行为树语义入口</summary>
    public abstract void Die();

    /// <summary>按玩家距离激活/失活：激活管理（AINpcService 调用）</summary>
    public abstract void SetActive(bool active_);

    #region 视线范围可视化
    /// <summary>编辑器 Gizmo：绘制视锥（含可视半角内/外渐变色）与玩家激活范围球。</summary>
    protected virtual void OnDrawGizmos()
    {
        Transform origin = _lookStartPoint != null ? _lookStartPoint : transform;
        Vector3 pos = origin.position;
        Vector3 fwd = origin.forward;

        // 视锥：以可视半角为边界，扇区用半透明面 + 中心轴 + 两边界射线表达
        float halfAngle = _viewAngle * 0.5f;
        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);
        Vector3 leftDir = leftRot * fwd;
        Vector3 rightDir = rightRot * fwd;

        // 扇形网格（扇区面）
        Gizmos.color = new Color(0f, 1f, 0f, 0.12f);
        int segments = 32;
        Vector3 prev = pos;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float ang = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd;
            Vector3 cur = pos + dir * _viewDistance;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
        Gizmos.DrawLine(pos, pos + leftDir * _viewDistance);
        Gizmos.DrawLine(pos, pos + rightDir * _viewDistance);

        // 中心视轴（实线，亮绿）
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + fwd * _viewDistance);
        // 两边界（暗绿虚化效果用半透明）
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        Gizmos.DrawLine(pos, pos + leftDir * _viewDistance);
        Gizmos.DrawLine(pos, pos + rightDir * _viewDistance);

        // 视距端点弧（用细线圆近似，仅表意）
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector3 last = pos + leftDir * _viewDistance;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float ang = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd;
            Vector3 cur = pos + dir * _viewDistance;
            Gizmos.DrawLine(last, cur);
            last = cur;
        }

        // 玩家激活范围（仅描线边框）
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _activationRadius);
    }
    #endregion
}

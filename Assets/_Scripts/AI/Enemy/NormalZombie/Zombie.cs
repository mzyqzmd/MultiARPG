using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 普通僵尸敌人（AINPC 子类）：Idle→(看见)Walk→(受伤)Run→(进攻击范围)等待后攻击→(死亡)Dead。
/// Host 端跑行为树与 NavMesh；攻击手部碰撞体在攻击窗口由动画帧事件开/关，命中玩家走 NetworkHub 受击中转。
/// </summary>
public class Zombie : AINPC
{
    [Header("攻击配置")]
    [SerializeField] private Collider _leftHandHit;
    [SerializeField] private Collider _rightHandHit;

    public ZombieStatsControl ZombieStatsControl => _statsControl as ZombieStatsControl;

    protected override void Awake()
    {
        base.Awake();
        _leftHandHit.enabled = false;
        _rightHandHit.enabled = false;
        _navAgent.speed = ZombieStatsControl.GetMoveSpeed(false);
        _navAgent.angularSpeed = ZombieStatsControl.RotationSpeed;
        _navAgent.stoppingDistance = 1f;
    }

    #region 攻击
    /// <summary>由行为树 ZombieAttack 节点调用：按玩家相对方位算 4 档方向后经桥接触发攻击动画（Root Motion 接管）。</summary>
    public override void PerformAttack()
    {
        int dir = ComputeAttackDirection();
        AnimBridge.TriggerAttack(dir);
    }

    /// <summary>计算攻击方向：玩家相对僵尸本地坐标系的方位（0前/1后/2右/3左）。</summary>
    private int ComputeAttackDirection()
    {
        if (TargetPlayer == null) return 0;
        Vector3 local = transform.InverseTransformDirection((TargetPlayer.position - transform.position).normalized);
        if (local.z >= Mathf.Abs(local.x) && local.z >= 0) return 0; // 前
        if (local.z < 0) return 1;                                  // 后
        return local.x >= 0 ? 2 : 3;                               // 右/左
    }

    /// <summary>攻击窗口开启（动画帧事件调用）：启用手部碰撞体。</summary>
    public void EnableHandHit() => SetHandHit(true);

    /// <summary>攻击窗口关闭（动画帧事件调用）：禁用手部碰撞体并交还 Agent 位移控制。</summary>
    public void DisableHandHit()
    {
        SetHandHit(false);
        AnimBridge.EndAttack();
    }

    private void SetHandHit(bool enabled_)
    {
        if (_leftHandHit != null) _leftHandHit.enabled = enabled_;
        if (_rightHandHit != null) _rightHandHit.enabled = enabled_;
    }

    private void OnTriggerEnter(Collider other_)
    {
        if (!IsServer) return;
        if (NetworkHub.Instance == null) return;
        var target = other_.GetComponent<Character>();
        if (target == null || target == this) return;
        var source = StatsControl as IEffectSource;
        var effectTarget = target.StatsControl as IEffectTarget;
        var damage = new DamageInfo(ZombieStatsControl.AttackDamage, 0, 0, 0, 0,
            other_.ClosestPoint(transform.position), source, effectTarget);
        NetworkHub.Instance.RequestDamageServerRpc(NetworkObjectId, target.OwnerClientId, damage);
    }
    #endregion

    #region 移动速度
    /// <summary>行为树追击分支按是否受伤切换走/跑速。</summary>
    public override void ApplyMoveSpeed(bool running_)
        => _navAgent.speed = ZombieStatsControl.GetMoveSpeed(running_);
    #endregion
}

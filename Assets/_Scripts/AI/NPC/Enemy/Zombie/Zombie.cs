using NodeCanvas.Tasks.Conditions;
using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 普通僵尸敌人（AINPC 子类）：Idle→(看见)Walk→(受伤)Run→(进攻击范围)等待后攻击→(死亡)Dead。
/// Host 端跑行为树与 NavMesh；攻击手部碰撞体在攻击窗口由动画帧事件开/关，命中玩家走 NetworkHub 受击中转。
/// </summary>
public class Zombie : AINPC
{
    [Header("基础配置")]
    [SerializeField] protected ZombieAnimControl _animControl;
    [Header("攻击配置")]
    [SerializeField] private Collider _leftHandHit;
    [SerializeField] private Collider _rightHandHit;

    public ZombieAnimControl AnimationControl => _animControl;
    public ZombieStatsControl ZombieStatsControl => _statsControl as ZombieStatsControl;

    /// <summary>攻击动画结束通知（由 DisableHandHit 末帧链路触发，供行为树节点收口）。</summary>
    public Action OnAttackEnded;

    protected override void Awake()
    {
        base.Awake();
        _leftHandHit.enabled = false;
        _rightHandHit.enabled = false;
        _navAgent.speed = ZombieStatsControl.GetMoveSpeed(false);
        _navAgent.angularSpeed = ZombieStatsControl.RotationSpeed;
        _navAgent.stoppingDistance = 1f;
    }

    #region 攻击窗口
    /// <summary>攻击窗口开启（动画帧事件调用）：启用手部碰撞体。</summary>
    public void EnableHandHit()
    {
        _leftHandHit.enabled = true;
        _rightHandHit.enabled = true;
    }

    /// <summary>攻击窗口关闭（动画帧事件调用）：禁用手部碰撞体并交还 Agent 位移控制。</summary>
    public void DisableHandHit()
    {
        _leftHandHit.enabled = false;
        _rightHandHit.enabled = false;
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



    #region 死亡
    public override void Die()
    {
        if (IsServer && _btOwner.isRunning) _btOwner.StopBehaviour();
        _navAgent.ResetPath();
        _animControl.SetDead();
    } 
    #endregion

    #region 受击反馈
    public override void OnHitReceived(DamageInfo damage_, Vector3 contactPoint_)
    {
        _animControl.SetHit(ComputeHitDirection(contactPoint_));
        base.OnHitReceived(damage_, contactPoint_);
    }
    /// <summary>计算受击方向：接触点相对本地坐标系的方位（0前/1后/2右/3左）。</summary>
    private int ComputeHitDirection(Vector3 contactPoint_)
    {
        Vector3 local = transform.InverseTransformDirection((contactPoint_ - transform.position).normalized);
        if (local.z >= Mathf.Abs(local.x) && local.z >= 0) return 0; // 前
        if (local.z < 0) return 1;                                  // 后
        return local.x >= 0 ? 2 : 3;                                // 右/左
    }
    #endregion

    #region 按玩家距离激活/失活
    public override void SetActive(bool active_)
    {
        if (active_ == _active) return;
        _active = active_;
        if (active_)
        {
            _navAgent.enabled = true;
            _animControl.SetIdle();
            if (IsServer && _btOwner.isPaused) _btOwner.StartBehaviour();
        }
        else
        {
            if (IsServer && _btOwner.isRunning) _btOwner.StopBehaviour();
            _navAgent.ResetPath();
            _animControl.SetIdle();
        }
    } 
    #endregion
}

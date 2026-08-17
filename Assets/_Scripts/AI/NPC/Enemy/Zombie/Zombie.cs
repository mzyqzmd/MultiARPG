using NodeCanvas.Tasks.Conditions;
using System;
using System.Collections;
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
    [SerializeField] private ZombieHandHit _leftHandHit;
    [SerializeField] private ZombieHandHit _rightHandHit;

    public ZombieAnimControl AnimationControl => _animControl;
    public ZombieStatsControl ZombieStatsControl => _statsControl as ZombieStatsControl;

    protected override void Awake()
    {
        base.Awake();
        _leftHandHit.Disable();
        _rightHandHit.Disable();
        _navAgent.speed = ZombieStatsControl.GetMoveSpeed(false);
        _navAgent.angularSpeed = ZombieStatsControl.RotationSpeed;
        _navAgent.stoppingDistance = 1f;
    }

    #region 攻击窗口
    /// <summary>攻击窗口开启（行为树节点调用）：启用手部碰撞体。</summary>
    public void EnableHandHit()
    {
        _leftHandHit.Enable();
        _rightHandHit.Enable();
    }

    /// <summary>攻击窗口关闭（行为树/安全网调用）：禁用手部碰撞体。</summary>
    public void DisableHandHit()
    {
        _leftHandHit.Disable();
        _rightHandHit.Disable();
    }
    #endregion

    #region 死亡
    public override void Die()
    {
        if (IsServer && _btOwner.isRunning) _btOwner.StopBehaviour();
        _navAgent.ResetPath();
        _animControl.SetDead();
        if (IsServer) StartCoroutine(DestroyAfterDelay());
    }

    /// <summary>Host 端计时：死亡动画播完后经 NetworkObject.Despawn 同步销毁所有客户端实例。</summary>
    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(4f);
        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
    #endregion

    #region 受击反馈
    public override void OnHitReceived(DamageInfo damage_, Vector3 contactPoint_)
    {
        _animControl.SetHit(contactPoint_);
        base.OnHitReceived(damage_, contactPoint_);
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

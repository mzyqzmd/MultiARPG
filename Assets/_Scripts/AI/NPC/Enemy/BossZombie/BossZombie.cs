using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 僵尸（AINPC 子类）：第一阶段（P1 持武器）Host 权威 AI。
/// P2（丢武器狂暴/冲击波）尚未实现，仅保留阶段枚举与终态，阶段切换钩子待后续接入。
/// 网络层下沉到独立 NetworkBehaviour，本类为零 NV 领域类。
/// </summary>
public class BossZombie : AINPC
{
    [Header("动画桥")]
    [SerializeField] private BossZombieAnimControl _animControl;
    [Header("碰撞体（P1 近战）")]
    [SerializeField] private BossZombieColliderHit _weaponHit;
    [SerializeField] private BossZombieColliderHit _rightHandHit;
    [Header("音频特效表现")]
    [SerializeField] private SO_BossZombiePresentation _BossZombiePresentation;

    public BossZombieStatsControl BossZombieStatsControl => _statsControl as BossZombieStatsControl;
    public BossZombieAnimControl AnimationControl => _animControl;
    public SO_BossZombiePresentation BossZombiePresentation => _BossZombiePresentation;
    /// <summary>当前阶段（只读，供 BT 读取）。</summary>
    public EBossZombiePhase Phase { get; private set; } = EBossZombiePhase.Phase1;
    /// <summary>Boss 被击败事件：死亡末尾触发，未来掉落/任务系统订阅此事件接入，无需改本类内部。</summary>
    public event Action OnBossDefeated;

    protected override void Awake()
    {
        base.Awake();
        _navAgent.speed = BossZombieStatsControl.GetMoveSpeed(false);
        _navAgent.angularSpeed = BossZombieStatsControl.RotationSpeed;
        _navAgent.stoppingDistance = 1.5f;
        _weaponHit.Disable();
        _rightHandHit.Disable();
    }

    #region 碰撞体细分开关（供 BT 攻击节点调用）
    public void EnableWeaponHit()
    {
        _weaponHit.Enable();
        _rightHandHit.Enable();
    }
    public void DisableWeaponHit()
    {
        _weaponHit.Disable();
        _rightHandHit.Disable();
    }
    #endregion

    #region 死亡
    public override void Die()
    {
        if (Phase == EBossZombiePhase.Dead) return;
        Phase = EBossZombiePhase.Dead;
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
        OnBossDefeated?.Invoke();
    }
    #endregion

    #region 受击反馈
    public override void OnHitReceived(string effectId_, float multiplier_, Vector3 contactPoint_, bool isStun_ = false)
    {
        _animControl.ForceEndAttack();
        base.OnHitReceived(effectId_, multiplier_, contactPoint_, isStun_);
        if (!_statsControl.IsDead)
            _animControl.SetHit(contactPoint_);
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

using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 僵尸（AINPC 子类）：两阶段（P1 持武器 / P2 丢武器狂暴）Host 权威 AI。
/// 网络层下沉到 BossZombieNetworkSync（独立 NetworkBehaviour），本类为零 NV 领域类。
/// 阶段切换（HP≤50%）由 NetworkSync 阈值通知触发，经由 EnterTransition→OnDownComplete→OnRiseComplete 编排。
/// </summary>
public class BossZombie : AINPC
{
    [Header("动画桥")]
    [SerializeField] private BossZombieAnimControl _animControl;
    [Header("碰撞体（按阶段分组启用）")]
    [SerializeField] private BossZombieColliderHit _weaponHit;
    [SerializeField] private BossZombieColliderHit _leftHandHit;
    [SerializeField] private BossZombieColliderHit _rightHandHit;
    [SerializeField] private BossZombieColliderHit _leftFootHit;
    [SerializeField] private BossZombieColliderHit _rightFootHit;
    [Header("武器视觉（P2 丢武器）")]
    [SerializeField] private Transform _weaponVisual;

    public BossZombieStatsControl BossZombieStatsControl => _statsControl as BossZombieStatsControl;
    public SO_BossZombiePresentation BossZombiePresentation => _presentation as SO_BossZombiePresentation;
    public BossZombieAnimControl AnimationControl => _animControl;
    /// <summary>当前阶段（只读，供 NetworkSync / BT 读取）。</summary>
    public EBossZombiePhase Phase => _phase;
    /// <summary>Boss 被击败事件：死亡末尾触发，未来掉落/任务系统订阅此事件接入，无需改本类内部。</summary>
    public event System.Action OnBossDefeated;

    private EBossZombiePhase _phase = EBossZombiePhase.Phase1;
    /// <summary>受击打断免疫效果 id：累计受击达阈值时施加，将 HitDuration 压到 0（Timed 窗口内不打断攻击、不播受击）。</summary>
    private const string HitStunEffectId = "BossZombieHitStun";
    private int _hitCount;

    protected override void Awake()
    {
        base.Awake();
        _navAgent.speed = BossZombieStatsControl.GetMoveSpeed(false);
        _navAgent.angularSpeed = BossZombieStatsControl.RotationSpeed;
        _navAgent.stoppingDistance = 1.5f;
        // 初始仅武器碰撞体待命（P1），手脚碰撞体保持禁用
        _weaponHit.Disable();
        _leftHandHit.Disable();
        _rightHandHit.Disable();
        _leftFootHit.Disable();
        _rightFootHit.Disable();
    }

    #region 阶段切换编排（由 NetworkSync 阈值通知触发）
    /// <summary>HP 过 50% 由 NetworkSync 调用：冻结 BT、播 down、禁用武器碰撞体、临时关 Nav 旋转。</summary>
    public void OnPhaseThresholdCrossed()
    {
        if (_phase != EBossZombiePhase.Phase1) return;
        _phase = EBossZombiePhase.Transitioning;
        if (IsServer && _btOwner.isRunning) _btOwner.StopBehaviour();
        _navAgent.ResetPath();
        _navAgent.updateRotation = false;
        DisableAllHit();
        // 通知动画桥播 down（动画事件 OnDownComplete 回调驱动下一步）
        _animControl.PlayPhaseDown();
    }

    /// <summary>down01 末帧动画事件回调：等待演出结束，丢武器并切到 P2。</summary>
    public void OnDownComplete()
    {
        if (_phase != EBossZombiePhase.Transitioning) return;
        DropWeapon();
        _phase = EBossZombiePhase.Phase2;
        _navAgent.angularSpeed = BossZombieStatsControl.RotationSpeed * 1.6f; // P2 狂暴转向更快
        _animControl.PlayPhaseRise();
    }

    /// <summary>rise01 末帧动画事件回调：还原 Nav 旋转、解冻 BT、P2 手脚碰撞体生效。</summary>
    public void OnRiseComplete()
    {
        if (_phase != EBossZombiePhase.Phase2) return;
        _navAgent.updateRotation = true;
        EnableHandHit();
        EnableFootHit();
        if (IsServer && !_btOwner.isRunning) _btOwner.StartBehaviour();
    }

    /// <summary>丢武器（仅视觉表现）：隐藏武器子物体，不生成 NetworkObject、不联网武器实体。</summary>
    private void DropWeapon()
    {
        if (_weaponVisual != null) _weaponVisual.gameObject.SetActive(false);
        _weaponHit.Disable();
    }
    #endregion

    #region 碰撞体细分开关（供 BT 攻击节点按招式调用）
    public void EnableWeaponHit() => _weaponHit.Enable();
    public void EnableHandHit() { _leftHandHit.Enable(); _rightHandHit.Enable(); }
    public void EnableFootHit() { _leftFootHit.Enable(); _rightFootHit.Enable(); }
    public void DisableAllHit()
    {
        _weaponHit.Disable();
        _leftHandHit.Disable();
        _rightHandHit.Disable();
        _leftFootHit.Disable();
        _rightFootHit.Disable();
    }
    #endregion

    #region 死亡
    public override void Die()
    {
        if (_phase == EBossZombiePhase.Dead) return;
        _phase = EBossZombiePhase.Dead;
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
    public override void OnHitReceived(DamageInfo damage_, Vector3 contactPoint_)
    {
        if (++_hitCount >= BossZombieStatsControl.HitStunThreshold)
        {
            _hitCount = 0;
            BossZombieStatsControl.ApplyEffect(EffectService.GetEffect(HitStunEffectId), BossZombieStatsControl);
        }
        _animControl.ForceEndAttack();
        base.OnHitReceived(damage_, contactPoint_);
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

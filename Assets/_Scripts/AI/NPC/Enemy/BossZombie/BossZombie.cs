using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Boss 僵尸（AINPC 子类）：第一阶段（P1 持武器）Host 权威 AI。
/// P2 丢武器狂暴：HP 阈值触发 Transitioning（丢武器演出 + 隐藏武器）→ Phase2（拳脚近战 + Ground 冲击波）。
/// 阶段流转与武器可见性均为 server 权威，位移经 NetworkTransform、动画经 NetworkAnimator 同步。
/// </summary>
public class BossZombie : AINPC
{
    [Header("动画桥")]
    [SerializeField] private BossZombieAnimControl _animControl;
    [Header("碰撞体（P1 近战）")]
    [SerializeField] private BossZombieColliderHit _weaponHit;
    [SerializeField] private BossZombieColliderHit _rightHandHit;
    [Header("碰撞体（P2 拳脚）")]
    [SerializeField] private BossZombieColliderHit _leftHandHit;
    [SerializeField] private BossZombieColliderHit _footLeftHit;
    [SerializeField] private BossZombieColliderHit _footRightHit;
    [Header("音频特效表现")]
    [SerializeField] private SO_BossZombiePresentation _BossZombiePresentation;
    [Header("Ground 冲击波（P2 仅 Ground 触发）")]
    [SerializeField] private BossZombieShockWave _shockWavePrefab;
    [Header("阶段切换")]
    [Tooltip("P2 触发阈值：当前血量 <= 最大值 * 此比例时进入 Transitioning 演出再切 Phase2。")]
    [SerializeField] private float _phase2HealthThreshold = 0.5f;
    [Tooltip("阶段切换演出时长（秒）：需与 AC 的 Down→Rise 自然时长对齐（down01×0.9 + rise01×0.9423 ≈ 6.48）。prefab 已序列化旧值，改脚本默认值不会自动同步，需在 Inspector 手动改或 Revert。")]
    [SerializeField] private float _transitionDuration = 6.5f;
    [Tooltip("Boss对应控制的雾墙")]
    [SerializeField] private FogWall _fogWall;

    /// <summary>武器物体显隐（server 权威）：P2 丢武器后跨端隐藏。武器模型与碰撞体同在 _weaponHit 所在物体上。</summary>
    private readonly NetworkVariable<bool> _weaponVisible = new(true);
    /// <summary>血量广播镜像（server 权威）：Host 端 _statsControl.CurrentHealth 的跨端镜像，是 UI 层唯一的血量读取源。
    /// 非独立数值——仅在 Host 端由 OnHealthChanged 回调同步写入，客户端副本恒满血故不可直读 StatsControl。</summary>
    private readonly NetworkVariable<int> _syncedHealth = new(0);

    private UIService _uiService;
    private UI_BossZombieInfoPanel _infoPanel;

    public BossZombieStatsControl BossZombieStatsControl => _statsControl as BossZombieStatsControl;
    public BossZombieAnimControl AnimationControl => _animControl;
    public SO_BossZombiePresentation BossZombiePresentation => _BossZombiePresentation;
    /// <summary>当前阶段（只读，供 BT 读取）。</summary>
    public EBossZombiePhase Phase { get; private set; } = EBossZombiePhase.Phase1;
    /// <summary>跨端同步的当前血量（只读，供 UI 读取）。</summary>
    public int SyncedHealth => _syncedHealth.Value;
    /// <summary>Boss 被击败事件：死亡末尾触发，未来掉落/任务系统订阅此事件接入，无需改本类内部。</summary>
    public event Action OnBossDefeated;
    /// <summary>跨端血量变更事件（NV 回调转发）：UI 层订阅此事件即可，无需依赖 Unity.Netcode 类型。</summary>
    public event Action<int> OnSyncedHealthChanged;

    protected override void Awake()
    {
        base.Awake();
        _uiService = Services.Get<UIService>();
        _navAgent.speed = BossZombieStatsControl.GetMoveSpeed(false);
        _navAgent.angularSpeed = BossZombieStatsControl.RotationSpeed;
        _navAgent.stoppingDistance = 1.5f;
        _statsControl.OnCurrentHealthChanged += OnHealthChanged;
        DisableWeaponHit();
        DisableHandAndFootHit();
    }

    #region 网络同步（武器物体显隐）
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _weaponVisible.OnValueChanged += OnWeaponVisibleChanged;
        if (IsServer) _weaponVisible.Value = true;
        ApplyWeaponVisible(_weaponVisible.Value);

        _syncedHealth.OnValueChanged += OnSyncedHealthValueChanged;
        if (IsServer) _syncedHealth.Value = _statsControl.CurrentHealth;

        _infoPanel = _uiService.SpawnUiView<UI_BossZombieInfoPanel>("BossZombieInfoPanel", EUILayerCanvas.Hud);
        _infoPanel.Initialize(this);
    }

    public override void OnNetworkDespawn()
    {
        _weaponVisible.OnValueChanged -= OnWeaponVisibleChanged;
        _syncedHealth.OnValueChanged -= OnSyncedHealthValueChanged;
        if (_infoPanel != null) Destroy(_infoPanel.gameObject);
        _infoPanel = null;
        base.OnNetworkDespawn();
    }

    private void OnSyncedHealthValueChanged(int _, int current_) => OnSyncedHealthChanged?.Invoke(current_);

    private void OnWeaponVisibleChanged(bool _, bool visible_) => ApplyWeaponVisible(visible_);

    private void ApplyWeaponVisible(bool visible_)
    {
        if (_weaponHit != null) _weaponHit.gameObject.SetActive(visible_);
    }

    /// <summary>设置武器物体显隐（server 权威，跨端同步）：P2 丢武器演出中/结束时调用。</summary>
    public void SetWeaponVisible(bool visible_)
    {
        if (IsServer) _weaponVisible.Value = visible_;
    }
    #endregion

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
    public void EnableHandAndFootHit()
    {
        _leftHandHit.Enable();
        _rightHandHit.Enable();
        _footLeftHit.Enable();
        _footRightHit.Enable();
    }
    public void DisableHandAndFootHit()
    {
        _leftHandHit.Disable();
        _rightHandHit.Disable();
        _footLeftHit.Disable();
        _footRightHit.Disable();
    }
    #endregion

    #region 阶段切换（HP 阈值驱动，最内聚）
    /// <summary>本地血量权威回调：跨阈值一次切 Transitioning→Phase2；仅 Host 驱动切阶段（Stage2 行为树仅 Host 跑）。</summary>
    private void OnHealthChanged(int _, int currentHealth_)
    {
        if (!IsServer) return;
        // 广播镜像与本地权威数值同步：放在死亡判定之前，保证归零也能同步出去。
        _syncedHealth.Value = currentHealth_;
        // 血量归零：任何阶段（含 P2）都应死亡；Transitioning 演出期间不打断，待演出自然结束再死（EnterTransitioning 末尾已置 Phase2）。
        if (_statsControl.IsDead && Phase != EBossZombiePhase.Dead && Phase != EBossZombiePhase.Transitioning)
        {
            Die();
            return;
        }
        if (Phase != EBossZombiePhase.Phase1) return;
        if (currentHealth_ > BossZombieStatsControl.MaxHealth * _phase2HealthThreshold) return;
        EnterTransitioning();
    }

    /// <summary>阶段切换演出：停手/禁武器碰撞体 + 播放丢武器动画（AC 的 Down/Rise 过渡由用户编辑器接线），演出结束置 Phase2。</summary>
    private void EnterTransitioning()
    {
        Phase = EBossZombiePhase.Transitioning;
        DisableWeaponHit();
        if (IsServer && _btOwner.isRunning) _btOwner.PauseBehaviour();
        _navAgent.ResetPath();
        _animControl.SetTransitioning();
        if (IsServer) StartCoroutine(TransitionToPhase2());
    }

    private IEnumerator TransitionToPhase2()
    {
        // 兜底隐藏武器：即使 down01 未配 DropWeaponEvent 动画事件，也保证起身时武器已丢弃。
        SetWeaponVisible(false);
        yield return new WaitForSeconds(_transitionDuration);
        Phase = EBossZombiePhase.Phase2;
        if (IsServer && _btOwner.isPaused) _btOwner.StartBehaviour();
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
        if (IsServer)
        {
            StartCoroutine(DestroyAfterDelay());
            DeactivateFogWallsClientRpc();
        }
    }

    /// <summary>死亡确认后向所有客户端广播：关闭场景内雾墙阻挡检测，玩家可自由进出 boss 房间。</summary>
    [ClientRpc]
    private void DeactivateFogWallsClientRpc() =>
        _fogWall.Deactivate();

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
        // 阶段切换演出优先于受击表现：base 内扣血可能已触发 EnterTransitioning（Phase 变为 Transitioning），
        // 此时若再 SetTrigger(Hit)，残留的 Hit 会把刚起步的 Down 过渡抢回 Hit 状态，倒下动画播不出来。
        if (!_statsControl.IsDead && Phase != EBossZombiePhase.Transitioning)
            _animControl.SetHit(contactPoint_);
    }
    #endregion

    #region 朝向
    /// <summary>水平面向玩家（忽略 Y 轴）：攻击/突进起手前强制对准，避免 Root Motion 沿错误朝向冲出。</summary>
    public void FaceTarget()
    {
        if (TargetPlayer == null) return;
        Vector3 dir = TargetPlayer.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }
    #endregion

    #region Ground 冲击波（P2 仅 Ground 触发）
    /// <summary>Ground 攻击动画末帧事件调用：Host 端 spawn 冲击波（半径由小到大扩张，命中玩家造成击晕）。</summary>
    public void SpawnGroundShockWave()
    {
        if (!IsServer || _shockWavePrefab == null) return;
        var wave = Instantiate(_shockWavePrefab, transform.position, Quaternion.identity);
        wave.Initialize(this, transform);
    }
    #endregion

    #region 按玩家距离激活/失活
    public override void SetActive(bool active_)
    {
        if (active_ == _active) return;
        _active = active_;
        if (active_)
        {
            if (IsServer)
            {
                _navAgent.enabled = true;
                _animControl.SetIdle();
                if (_btOwner.isPaused) _btOwner.StartBehaviour();
            }
            else
            {
                _navAgent.enabled = false;
            }
        }
        else
        {
            if (IsServer)
            {
                if (_btOwner.isRunning) _btOwner.StopBehaviour();
                _navAgent.ResetPath();
                _animControl.SetIdle();
            }
            else
            {
                _navAgent.enabled = false;
            }
        }
    }
    #endregion

    #region 信息面板驱动
    /// <summary>驱动源恒在 Boss 侧：面板 Hide 走 SetActive(false) 会停掉自身 Update，无法检测玩家重新进场，故由本类转发。显隐判断与插值逻辑全在 Presenter。</summary>
    protected override void Update()
    {
        base.Update();
        _infoPanel?.Tick(Time.deltaTime);
    }
    #endregion
}

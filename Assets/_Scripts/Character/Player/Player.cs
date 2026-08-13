using Cinemachine;
using UnityEngine;

public class Player : Character
{
    [Header("基础配置")]
    [SerializeField] private CinemachineFreeLook _freeLookCamera;

    [Header("玩家依赖组件")]
    [SerializeField] private PlayerAnimationControl _animEventControl;
    [SerializeField] private PlayerLockOnTarget _lockOnTarget;
    [SerializeField] private PlayerEquipmentControl _equipmentControl;
    [SerializeField] private PlayerSaveControl _saveControl;// 玩家存档控制（纯本地数据组装/还原）
    [SerializeField] private PlayerStateControl _stateControl;// 玩家状态控制（挂 Player.prefab 同对象，Inspector 拖入）
                                                              // 服务
    private AudioService _audioService;
    private UIService _uiService;
    private InputService _inputService;                 // 输入
    private PlayerCameraService _playerCameraService;   // 相机
    private GameSaveService _saveService;
    private SceneControlService _sceneControlService;

    //组件
    public new PlayerStatsControl StatsControl => _statsControl as PlayerStatsControl;
    public PlayerAnimationControl AnimationEventControl => _animEventControl;
    public PlayerLockOnTarget LockOnTarget => _lockOnTarget;
    public PlayerEquipmentControl EquipmentControl => _equipmentControl;
    public PlayerSaveControl SaveControl => _saveControl;
    public PlayerStateControl StateControl => _stateControl;

    //服务
    public AudioService AudioService => _audioService;
    public UIService UIService => _uiService;
    public InputService InputService => _inputService;
    public PlayerCameraService PlayerCameraService => _playerCameraService;
    public GameSaveService SaveService => _saveService;
    public SceneControlService SceneControlService => _sceneControlService;

    public override bool CanBeLocked => false;

    #region Awake 时初始化
    protected override void Awake()
    {
        base.Awake();
        _audioService = Services.Get<AudioService>();
        _uiService = Services.Get<UIService>();
        _inputService = Services.Get<InputService>();
        _playerCameraService = Services.Get<PlayerCameraService>();
        _saveService = Services.Get<GameSaveService>();
        _sceneControlService = Services.Get<SceneControlService>();
    }

    #endregion

    #region 玩家从网络中创建或销毁时执行
    public override void OnNetworkSpawn()
    {
        // 仅本地玩家启用跟随虚拟相机，避免多玩家 vcam 抢占
        _freeLookCamera.enabled = IsOwner;
        if (!IsOwner) return;
        //状态control和lockOn相互绑定
        _stateControl.OnLockBreakRequested += _lockOnTarget.ForceUnlock;
        _lockOnTarget.OnLockStateChanged += _stateControl.OnLockStateChange;
        //输入事件订阅（IsOwner 已可靠的时机，避免 Awake 阶段 IsOwner 为假导致订阅跳过）
        _stateControl.BindInput();
        _equipmentControl.BindInput();
        _lockOnTarget.BindInput();
        //给UI服务绑定
        _uiService.BindLocalPlayer(this);

        // 注册本地玩家数据提供方给 NetworkHub（供周期静默上报/Host 缓存刷新取最新数据）
        if (NetworkHub.Instance != null)
            NetworkHub.Instance.RegisterLocalDataProvider(() =>
            _saveControl.BuildPlayerSaveData(LocalPlayerId.Value));

        #region 网络存档相关（订阅 + 还原 + 进世界上报）
        // Host 自身存档还原：上线即读本地存档记录还原坐标/生命/世界时长（原 PlayerNetworkSaveControl.HostRestoreSelf 等价）
        if (IsHost)
        {
            var self = _saveService.GetPlayer(LocalPlayerId.Value);
            if (self != null)
            {
                _saveControl.RestoreFromPlayer(self);
                StatsControl.RestoreHealth(self.currentHealth);
                _saveControl.RestoreWorldTime(_saveService.GetWorld()?.secondsPlayed ?? 0f);
            }
            else
            {
                StatsControl.ApplyDefaultHealth();
                _saveService.UpsertPlayer(_saveControl.BuildPlayerSaveData(LocalPlayerId.Value));
            }
        }

        // 客人/Host 订阅 Hub 还原事件：收到 Host 下发的存档后写回本地（仅本端 owner 还原自身）
        if (NetworkHub.Instance != null)
            NetworkHub.Instance.OnPlayerRestore += OnPlayerRestored;

        // 进世界即上报自身存档（填充 Host 侧 _clientSaves，支撑断联兜底与客人读档还原）；Hub 未就绪时跳过不影响后续手动存档
        if (NetworkHub.Instance != null)
            NetworkHub.Instance.RequestJoin(_saveControl.BuildPlayerSaveData(LocalPlayerId.Value));

        // 订阅"即将返回主菜单"信号：Host 端协调各端存档，客人端仅上报自身
        EventBus<ReturningToMainMenuEvent>.Subscribe(OnReturningToMainMenu);
        #endregion
    }

    public override void OnNetworkDespawn()
    {
        _freeLookCamera.enabled = false;
        if (!IsOwner) return;
        //状态control和lockOn相互取消绑定
        _stateControl.OnLockBreakRequested -= _lockOnTarget.ForceUnlock;
        _lockOnTarget.OnLockStateChanged -= _stateControl.OnLockStateChange;
        //反订阅输入事件（与 OnNetworkSpawn 配对，防重 spawn 累积）
        _stateControl.UnbindInput();
        _equipmentControl.UnbindInput();
        _lockOnTarget.UnbindInput();
        #region 反订阅网络存档相关
        EventBus<ReturningToMainMenuEvent>.Unsubscribe(OnReturningToMainMenu);

        // 仅解绑本地玩家 UI
        _uiService.UnbindLocalPlayer();

        if (NetworkHub.Instance != null)
            NetworkHub.Instance.OnPlayerRestore -= OnPlayerRestored;
        #endregion
    }

    /// <summary>返回主菜单信号回调：Host 协调全体存档，客人仅上报自身。</summary>
    private void OnReturningToMainMenu(ReturningToMainMenuEvent _) => RequestSave();

    /// <summary>Hub 还原事件回调：客人收到 Host 下发存档后写回本地坐标与生命。</summary>
    private void OnPlayerRestored(PlayerSaveData data_)
    {
        if (!IsOwner) return;
        _saveControl.RestoreFromPlayer(data_);
        StatsControl.RestoreHealth(data_.currentHealth);
    }

    /// <summary>本地存档入口（Checkpoint 调用）：Host 协调全体（自身 + 各客人缓存 + 世界状态）落盘；客人仅上报自身。</summary>
    public bool RequestSave()
    {
        if (!IsOwner) return false;
        var self = _saveControl.BuildPlayerSaveData(LocalPlayerId.Value);
        if (IsHost)
        {
            NetworkHub.Instance.CoordinateSave(self, _saveControl.BuildWorldSaveData());
            return true;
        }
        if (NetworkHub.Instance == null)
            return false;
        NetworkHub.Instance.RequestSave(self);
        return true;
    }
    #endregion

    #region 受击入口（重写基类钩子：先播受击动画再 base 本地扣血）
    /// <summary>受击钩子重写：进受击态（按 contactPoint 选方向动画）后交由基类统一本地扣血，死亡链路由基类 OnCurrentHealthChanged 自然触发。</summary>
    public override void OnHitReceived(DamageInfo damage_, Vector3 contactPoint_)
    {
        _stateControl.EnterHitState(contactPoint_);
        base.OnHitReceived(damage_, contactPoint_);
    }
    #endregion
}

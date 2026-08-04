using Cinemachine;
using UnityEngine;

public class Player : Character
{
    [Header("测试")]
    [SerializeField] private BaseItem _mainWeapon;

    [Header("玩家配置")]
    [SerializeField] private SO_PlayerStats _soPlayerStats;
    [SerializeField] private CinemachineFreeLook _freeLookCamera;
    [SerializeField] private PlayerAnimationEventControl _animationControl; //动画控制

    [Header("装备挂载点")]
    [SerializeField] private Transform _leftHand;//左手
    [SerializeField] private Transform _rightHand;//右手
    [SerializeField] private Transform _leftHip;//左胯
    [SerializeField] private Transform _rightHip;//右胯
    [SerializeField] private Transform _back;//背后

    // 服务
    private InputService _inputService;
    private PlayerCameraService _playerCameraService;
    private AudioService _audioService;
    private UIService _uiService;
    private GameSaveService _saveService;
    private SceneControlService _sceneControlService;
    private NetworkService _networkService;
    private ItemService _itemService;

    // 组件
    private PlayerStatsControl _playerStatsControl;//玩家数据控制(含耐力)
    private PlayerSaveControl _saveControl;// 玩家存档控制（纯本地数据组装/还原）

    // 状态
    private PlayerStateControl _stateControl;// 玩家状态控制
    private PlayerGroundCheck _groundCheck;// 玩家地面检测
    private PlayerVerticalMotionControl _verticalMotion;// 玩家垂直运动控制

    private PlayerEquipmentControl _equipmentControl;// 玩家装备控制
    private BaseItem _pendingItem;// 当前切换流程中待提交挂载的物品
    private EEquipmentSlot _pendingSlot;// 当前切换流程的目标槽位

    /// <summary>本地玩家属性控制(含耐力与生命，供 UI 访问)。</summary>
    public PlayerStatsControl PlayerStatsControl => _playerStatsControl;

    #region Awake 时初始化
    protected override void Awake()
    {
        base.Awake();
        #region 获取服务
        _inputService = Services.Get<InputService>();
        _playerCameraService = Services.Get<PlayerCameraService>();
        _audioService = Services.Get<AudioService>();
        _uiService = Services.Get<UIService>();
        _saveService = Services.Get<GameSaveService>();
        _sceneControlService = Services.Get<SceneControlService>();
        _networkService = Services.Get<NetworkService>();
        _itemService = Services.Get<ItemService>();
        #endregion

        #region 注册组件
        // 属性控制
        _playerStatsControl = new PlayerStatsControl(_soPlayerStats);
        _statsControl = _playerStatsControl;// 供基类分流本地属性修正（同时作为 IEffectTarget / IEffectSource）

        // 存档数据组装/还原能力（纯本地，不含网络字段；写盘统一由 Host 侧 SaveService 单文件落盘，经 NetworkHub 编排）
        _saveControl = new PlayerSaveControl(_sceneControlService, transform, _playerStatsControl);

        //状态机控制
        _groundCheck = new PlayerGroundCheck(_characterController);
        _verticalMotion = new PlayerVerticalMotionControl(_playerStatsControl, _groundCheck);

        //装备控制（挂载权威，需在状态机之前实例化以便传入）
        _equipmentControl = new PlayerEquipmentControl(_leftHand, _rightHand, _leftHip, _rightHip, _back, this);

        _stateControl = new PlayerStateControl(_inputService, _playerCameraService,
            _characterController, _playerStatsControl, _anim, _audioService, _uiService,
            _vfxService, _verticalMotion, _equipmentControl);
        #endregion
    }

    #endregion

    #region Update 更新
    protected override void Update()
    {
        base.Update();   // 基类在 owner 端驱动持续效果 Tick

        if (!IsOwner) return;

        // Host 自身每帧刷 NetworkHub 本地缓存（数据新鲜度由 Player 保证，计时落盘仍由 Hub 集中驱动）
        if (IsHost)
            NetworkHub.Instance?.UpdateHostCache(_saveControl.BuildPlayerSaveData(LocalPlayerId.Value));

        _stateControl.Update();

        // 仅在世界场景累计游玩时长（供存档点 / 返回主菜单存档使用，主菜单不累计）
        _saveControl.TickPlayTime(Time.deltaTime);

        // 非跑步期间(walk/站立/闪避/跳跃/坠落)经过延迟自动回复耐力
        if (!_stateControl.IsRunning)
            _playerStatsControl.RecoverStamina(Time.deltaTime);
    }
    #endregion

    #region 玩家从网络中创建或销毁时执行
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 仅本地玩家启用跟随虚拟相机，避免多玩家 vcam 抢占
        _freeLookCamera.enabled = IsOwner;

        if (!IsOwner) return;

        #region 本地玩家初始化
        _playerStatsControl.OnExhausted += OnStaminaExhausted;
        _playerStatsControl.ResetStamina();

        _uiService.BindLocalPlayer(this);

        // 注册本地玩家数据提供方给 NetworkHub（供周期静默上报/Host 缓存刷新取最新数据）
        if (NetworkHub.Instance != null)
            NetworkHub.Instance.RegisterLocalDataProvider(() => _saveControl.BuildPlayerSaveData(LocalPlayerId.Value));

        // 热键栏：装备/使用物品（仅本地玩家触发拔刀/收刀表现）
        _inputService.OnHotbarTriggered += OnHotbarTriggered;
        //动画事件接收器：订阅两个 Animation Event 出口，由 Player 桥接编排（装备控制与动画控制互不依赖）
        _animationControl.OnToggleFrame += OnToggleFrame;
        _animationControl.OnAnimationFinished += OnAnimationFinished;

        #endregion

        #region 网络存档相关（订阅 + 还原 + 进世界上报）
        // Host 自身存档还原：上线即读本地存档记录还原坐标/生命/世界时长（原 PlayerNetworkSaveControl.HostRestoreSelf 等价）
        if (IsHost)
        {
            var self = _saveService.GetPlayer(LocalPlayerId.Value);
            if (self != null)
            {
                _saveControl.RestoreFromPlayer(self);
                _playerStatsControl.RestoreHealth(self.currentHealth);
                _saveControl.RestoreWorldTime(_saveService.GetWorld()?.secondsPlayed ?? 0f);
            }
            else
            {
                _playerStatsControl.ApplyDefaultHealth();
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

        //NOTE:测试用的（仅本地玩家订阅，避免远端副本误触发）
        _inputService.OnTestButtonClicked += TestButtonClicked;
    }

    public override void OnNetworkDespawn()
    {
        _freeLookCamera.enabled = false;

        if (!IsOwner) return;

        #region 反订阅本地玩家输入/动画
        _playerStatsControl.OnExhausted -= OnStaminaExhausted;

        _inputService.OnHotbarTriggered -= OnHotbarTriggered;
        _animationControl.OnToggleFrame -= OnToggleFrame;
        _animationControl.OnAnimationFinished -= OnAnimationFinished;

        //NOTE:测试用的（与 OnNetworkSpawn 订阅对称反订阅）
        _inputService.OnTestButtonClicked -= TestButtonClicked;
        #endregion

        #region 反订阅网络存档相关
        EventBus<ReturningToMainMenuEvent>.Unsubscribe(OnReturningToMainMenu);

        // 仅解绑本地玩家 UI
        _uiService.UnbindLocalPlayer();

        if (NetworkHub.Instance != null)
            NetworkHub.Instance.OnPlayerRestore -= OnPlayerRestored;
        #endregion
    }

    private void TestButtonClicked()
    {
        if (!IsOwner) return;
        Debug.Log("TestButtonClicked");
        //ApplyEffect("StatModifierEffect");
        //ApplyEffect("DamageEffect");
        //_stateControl.CombatLocked = !_stateControl.CombatLocked;
        _sceneControlService.ReturnToMainMenu();
        //ApplyEffect("HealEffect");
    }

    /// <summary>返回主菜单信号回调：Host 协调全体存档，客人仅上报自身。</summary>
    private void OnReturningToMainMenu(ReturningToMainMenuEvent _) => RequestSave();

    /// <summary>Hub 还原事件回调：客人收到 Host 下发存档后写回本地坐标与生命。</summary>
    private void OnPlayerRestored(PlayerSaveData data_)
    {
        if (!IsOwner) return;
        _saveControl.RestoreFromPlayer(data_);
        _playerStatsControl.RestoreHealth(data_.currentHealth);
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

    #region 装备切换控制
    /// <summary>热键栏回调：本地玩家切换指定槽位装备（拔刀/收刀表现），远端副本不响应。
    /// 仅做状态决策（翻转占用表）并播动画，真实挂载在 OnToggleFrame 帧事件提交。</summary>
    private void OnHotbarTriggered(EEquipmentSlot slot_)
    {
        if (!IsOwner) return;
        BaseItem item = _mainWeapon;//NOTE:临时测试用
        if (item == null) return; // 未提供则不切换
        _equipmentControl.Toggle(slot_);
        _pendingItem = item;
        _pendingSlot = slot_;
        _stateControl.IsBusy = true;
        _animationControl.Equip1HandWeapon();
    }

    /// <summary>动画切换落点帧：提交真实挂载/卸下，并清 IsBusy。</summary>
    private void OnToggleFrame()
    {
        if (_pendingItem == null) return;
        if (_equipmentControl.IsSlotTaken(_pendingSlot))
            _equipmentControl.Equip(_pendingItem);
        else
            _equipmentControl.Unequip(_pendingItem);
        _pendingItem = null;
    }

    /// <summary>动画播放完成：清除忙状态。</summary>
    private void OnAnimationFinished() => _stateControl.IsBusy = false;
    #endregion

    #region 力竭事件回调
    private void OnStaminaExhausted() =>
        _audioService.PlayAudio("Player", "ExhaustedBreath", transform.position);
    #endregion

    #region 受击入口（重写基类钩子：先播受击动画再 base 本地扣血）
    /// <summary>受击钩子重写：进受击态（按 contactPoint 选方向动画）后交由基类统一本地扣血，死亡链路由基类 OnCurrentHealthChanged 自然触发。</summary>
    public override void OnHitReceived(DamageInfo damage_, Vector3 contactPoint_)
    {
        _stateControl.EnterHitState(contactPoint_);
        base.OnHitReceived(damage_, contactPoint_);
    }
    #endregion

    #region 应用焦点切换时恢复输入
    private void OnApplicationFocus(bool focus_)
    {
        if (focus_)
        {
            if (_sceneControlService.CurrentActiveScene.name == SceneControlService.SceneMainMenu)
                _inputService.EnableUIMode();
            else
                _inputService.EnablePlayerMode();
        }
        else
            _inputService.DisableAllMode();
    }


    #endregion
}

using Cinemachine;
using UnityEngine;

public class Player : Character
{
    [Header("玩家配置")]
    [SerializeField] private SO_PlayerStats _soPlayerStats;
    [SerializeField] private CinemachineFreeLook _freeLookCamera;
    [SerializeField] private PlayerNetworkSaveControl _playerNetworkSaveControl;//网络存档
    [SerializeField] private PlayerNetworkStatsControl _playerNetworkStatsControl;//玩家网络属性控制

    // 服务
    private InputService _inputService;
    private PlayerCameraService _playerCameraService;
    private AudioService _audioService;
    private UIService _uiService;
    private GameSaveService _saveService;
    private SceneControlService _sceneControlService;

    // 组件
    private PlayerStatsControl _playerStatsControl;//玩家数据控制(含耐力)

    private PlayerSaveControl _saveControl;// 玩家存档控制

    private PlayerStateControl _stateControl;// 玩家状态控制
    private PlayerGroundCheck _groundCheck;// 玩家地面检测
    private PlayerVerticalMotionControl _verticalMotion;// 玩家垂直运动控制

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
        #endregion

        #region 注册组件
        _playerStatsControl = new PlayerStatsControl(_soPlayerStats);
        _playerStatsControl.OnExhausted += OnStaminaExhausted;
        _statsControl = _playerStatsControl;// 供基类分流本地属性修正
        _effectSource = _playerStatsControl;// 攻击者来源（供攻击结算读取实时攻击属性）
        _networkStatsControl = _playerNetworkStatsControl;

        // 存档数据组装/还原能力（纯本地，不含网络字段；写盘统一由 Host 侧 SaveService 单文件落盘）
        _saveControl = new PlayerSaveControl(_sceneControlService, transform, _playerStatsControl);
        _playerNetworkSaveControl.Initialize(_saveService, _saveControl, _playerStatsControl);

        _groundCheck = new PlayerGroundCheck(_characterController);
        _verticalMotion = new PlayerVerticalMotionControl(_playerStatsControl, _groundCheck);
        _stateControl = new PlayerStateControl(_inputService, _playerCameraService,
            _characterController, _playerStatsControl, _anim, _audioService, _uiService, _verticalMotion);
        #endregion

        //NOTE:测试用的
        _inputService.OnTestButtonClicked += TestButtonClicked;
    }
    #endregion
    private void TestButtonClicked()
    {
        Debug.Log("TestButtonClicked");
        ApplyEffectServerRpc("aaa");
        ApplyEffectServerRpc("bbb");
        //ApplyEffectServerRpc("ccc");
    }

    #region Update 更新
    protected override void Update()
    {
        base.Update();   // 基类在 server 端驱动持续效果 Tick

        if (!IsOwner) return;

        _stateControl.Update();

        // 仅在世界场景累计游玩时长（供存档点 / 返回主菜单存档使用，主菜单不累计）
        _saveControl.TickPlayTime(Time.deltaTime);

        // 跑步缓慢消耗耐力；否则(walk/站立/闪避后)经过延迟自动回复
        if (_stateControl.IsRunning)
            _playerStatsControl.DrainStamina(_playerStatsControl.RunStaminaDrain, Time.deltaTime);
        else
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
        _playerStatsControl.ResetStamina();

        _uiService.BindLocalPlayer(this);

        // 订阅"即将返回主菜单"信号：Host 端协调各端存档，客人端仅上报自身
        EventBus<ReturningToMainMenuEvent>.Subscribe(OnReturningToMainMenu);
    }

    public override void OnNetworkDespawn()
    {
        _freeLookCamera.enabled = false;

        if (IsOwner)
        {
            EventBus<ReturningToMainMenuEvent>.Unsubscribe(OnReturningToMainMenu);

            // 仅解绑本地玩家 UI
            _uiService.UnbindLocalPlayer();
        }
    }

    /// <summary>返回主菜单信号回调：Host 协调全体存档；客人仅上报自身。</summary>
    private void OnReturningToMainMenu(ReturningToMainMenuEvent _)
    {
        if (IsHost)
            _playerNetworkSaveControl.CoordinateSave();
        else
            _playerNetworkSaveControl.ReportSaveToHost();
    }
    #endregion

    #region 力竭事件回调
    private void OnStaminaExhausted() =>
        _audioService.PlayAudio("Player", "ExhaustedBreath", transform);
    #endregion

    #region 供场景 Checkpoint 调用：手动存档当前进度（仅 owner 端）。Host 仅存自身+世界，客人上报自身
    public void ManualSaveCheckpoint()
    {
        if (!IsOwner) return;
        if (IsHost)
            _playerNetworkSaveControl.CoordinateSave();
        else
            _playerNetworkSaveControl.ReportSaveToHost();
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

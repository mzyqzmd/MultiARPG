using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 组合根：全场唯一的引导 MonoBehaviour，在 Awake 中创建并登记各 C# 服务，
/// 解决"纯 C# 类无生命周期、Unity 预制体无法构造函数注入"的问题。
/// </summary>
public class GameRoot : MonoBehaviour
{
    [Header("数据库设置")]
    [SerializeField] private SO_AudioDatabase _audioDatabase;
    [SerializeField] private SO_VFXDatabase _vfxDatabase;
    [SerializeField] private SO_UIDatabase _uiDatabase;
    [SerializeField] private SO_EffectDatabase _effectDatabase;
    [Header("网络中转站预制体")]
    [SerializeField] private NetworkHub _networkHubPrefab;

    private static bool _alive;
    // Awake 被 _alive 守卫早退的重复实例不置该标志，使其 OnDestroy 跳过整套注销链，
    // 避免把持久实例登记进 Services 的服务引用清空（Scene_MainMenu 内嵌了第二颗 GameRoot）
    private bool _initialized;
    private SettingsSaveService _settingsService;
    private AudioService _audioService;
    private VFXService _vfxService;
    private DamageTextService _damageTextService;
    private InputService _inputService;
    private SceneControlService _sceneControlService;
    private NetworkService _networkService;
    private PlayerCameraService _playerCameraService;
    private UIService _uiService;
    private GameSaveService _saveService;
    private AINpcService _aiNpcService;
    private EffectService _effectService;

    private void Awake()
    {
        if (_alive)
        {
            Destroy(gameObject);
            return;
        }
        _alive = true;
        DontDestroyOnLoad(gameObject);
        Services.Clear();

        // 存档与全局设置共享同一份文件 IO 实现（SaveFileDataWriter 已泛型化，全项目复用）
        var dataWriter = new SaveFileDataWriter();
        RegisterSettingsService(dataWriter);   // 须在 AudioService 之前：其构造即 ApplyVolumes 依赖 SettingsService
        RegisterSaveService(dataWriter);
        RegisterEffectService();               // 须在所有运行时消费者（Character/Weapon/BaseItem）之前：它们 Awake 取 EffectService
        RegisterUIService();                   // 须在 SceneControlService 之前：场景服务构造注入 UIService 以驱动场景切换黑幕
        RegisterNetworkService();              // 须在 SceneControlService 之前：场景服务构造注入 NetworkService 以收归场景加载
        RegisterAudioService();
        RegisterVFXService();
        RegisterDamageTextService(); // 须在 UIService 与 EffectService 之后：构造注入两者（取飘字预制体 + 预结算伤害数值）
        RegisterInputService();
        RegisterSceneControlService();
        RegisterPlayerCameraService();
        RegisterAINpcService();
        _initialized = true;
    }

    private void OnDestroy()
    {
        if (!_initialized) return;
        _alive = false;
        UnregisterPlayerCameraService();
        UnregisterSceneControlService();
        UnregisterInputService();
        UnregisterAudioService();
        UnregisterVFXService();
        UnregisterDamageTextService();
        UnregisterNetworkService();
        UnregisterUIService();
        UnregisterEffectService();
        UnregisterSaveService();
        UnregisterSettingsService();
        UnregisterAINpcService();
        Services.Clear();
        EventBus<ReturningToMainMenuEvent>.Clear();
    }

    #region 注册/注销全局设置服务
    private void RegisterSettingsService(SaveFileDataWriter dataWriter_)
    {
        string settingsPath = Application.persistentDataPath + "/Settings/settings.setting";
        _settingsService = new SettingsSaveService(dataWriter_, settingsPath);
        Services.Register(_settingsService);
    }

    private void UnregisterSettingsService()
    {
        Services.Unregister<SettingsSaveService>();
    }
    #endregion

    #region 注册/注销存档服务
    private void RegisterSaveService(SaveFileDataWriter dataWriter_)
    {
        // 存档目录 persistentDataPath/Saves（单机双开按实例后缀隔离，后缀统一由 LocalPlayerId 解析）
        string saveDir = Application.persistentDataPath + "/Saves";
        string suffix = LocalPlayerId.InstanceSuffix;
        if (!string.IsNullOrEmpty(suffix))
            saveDir = Path.Combine(saveDir, suffix);
        _saveService = new GameSaveService(dataWriter_, saveDir);
        Services.Register(_saveService);
    }

    private void UnregisterSaveService()
    {
        Services.Unregister<GameSaveService>();
    }
    #endregion

    #region 注册/注销效果服务
    private void RegisterEffectService()
    {
        _effectService = new EffectService(_effectDatabase);
        Services.Register(_effectService);
    }

    private void UnregisterEffectService()
    {
        Services.Unregister<EffectService>();
    }
    #endregion

    #region 注册/注销 UI 服务
    private void RegisterUIService()
    {
        _uiService = new UIService(transform, _uiDatabase);
        Services.Register(_uiService);
    }

    private void UnregisterUIService()
    {
        Services.Unregister<UIService>();
    }
    #endregion

    #region 注册/注销网络服务
    private void RegisterNetworkService()
    {
        _networkService = new NetworkService(_networkHubPrefab, _uiService);
        Services.Register(_networkService);
    }

    private void UnregisterNetworkService()
    {
        Services.Unregister<NetworkService>();
    }
    #endregion

    #region 注册/注销音频服务
    public void RegisterAudioService()
    {
        _audioService = new AudioService(_audioDatabase, transform, _settingsService);
        Services.Register(_audioService);
    }
    public void UnregisterAudioService()
    {
        Services.Unregister<AudioService>();
    }
    #endregion

    #region 注册/注销视觉特效服务
    private void RegisterVFXService()
    {
        _vfxService = new VFXService(_vfxDatabase, transform);
        Services.Register(_vfxService);
    }

    private void UnregisterVFXService()
    {
        Services.Unregister<VFXService>();
    }
    #endregion

    #region 注册/注销伤害飘字服务
    private void RegisterDamageTextService()
    {
        _damageTextService = new DamageTextService(_uiService, _effectService, transform);
        Services.Register(_damageTextService);
    }

    private void UnregisterDamageTextService()
    {
        Services.Unregister<DamageTextService>();
    }
    #endregion

    #region 注册/注销输入服务
    private void RegisterInputService()
    {
        _inputService = new InputService();
        _inputService.EnableUIMode();
        _inputService.OnESCTriggered += HandleESCTriggered;
        Services.Register(_inputService);
    }

    private void UnregisterInputService()
    {
        if (_inputService != null)
        {
            _inputService.OnESCTriggered -= HandleESCTriggered;
            _inputService.Dispose();
        }
        Services.Unregister<InputService>();
    }

    // ESC / 手柄 Start 触发：面板已开则关闭并恢复输入，否则仅在世界场景中打开
    private void HandleESCTriggered()
    {
        if (_uiService.IsEscPanelOpen)
        {
            _uiService.CloseEscPanel();
            _inputService.EnablePlayerMode();
            return;
        }
        if (SceneManager.GetActiveScene().name == SceneControlService.SceneWorld)
            _uiService.OpenEscPanel();
    }
    #endregion

    #region 注册/注销场景控制服务
    public void RegisterSceneControlService()
    {
        // InputService / UIService / NetworkService / AudioService 已在本方法之前注册，可安全注入
        _sceneControlService = new SceneControlService(_inputService, _uiService, _networkService, _audioService);
        Services.Register(_sceneControlService);
    }

    public void UnregisterSceneControlService()
    {
        Services.Unregister<SceneControlService>();
    }
    #endregion

    #region 注册/注销玩家摄像机服务
    private void RegisterPlayerCameraService()
    {
        _playerCameraService = new PlayerCameraService();
        Services.Register(_playerCameraService);
    }

    private void UnregisterPlayerCameraService()
    {
        Services.Unregister<PlayerCameraService>();
    }
    #endregion

    #region 注册/注销 AI NPC 调度服务
    private void RegisterAINpcService()
    {
        _aiNpcService = new AINpcService();
        Services.Register(_aiNpcService);
    }

    private void UnregisterAINpcService()
    {
        Services.Unregister<AINpcService>();
    }
    #endregion

    private void Update() =>
        _aiNpcService.Tick();

    private void OnApplicationFocus(bool focus) =>
        _sceneControlService?.OnApplicationFocusChanged(focus);
}

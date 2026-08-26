using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 角色基类（NetworkBehaviour）
/// 网络外观层——StatsControl 数值本地权威（owner 端应用与驱动），本类仅负责：
/// 自身/环境效果本地施加入口、受击扣血钩子。
/// 自身不再实现 IEffectTarget（效果目标/来源已下沉到 CharacterStatsControl）。
/// _statsControl 由子类在 Awake 中赋值（如 Player 赋 PlayerStatsControl）。
/// </summary>
public abstract class Character : NetworkBehaviour, ILockableTarget
{
    [Header("Character基础配置")]
    [SerializeField] protected Animator _anim;
    [SerializeField] protected Transform _lockCenter;
    [SerializeField] protected CharacterStatsControl _statsControl;
    [Header("表现配置")]
    [SerializeField] protected SO_CharacterPresentation _presentation;

    protected VFXService _vfxService;
    protected AudioService _audioService;
    protected EffectService _effectService;

    public Animator Anim => _anim;
    // ILockableTarget
    /// <summary>是否可被锁定：死亡（本地血量归零）时自动不可锁定，复活后自动恢复。</summary>
    public virtual bool CanBeLocked => _statsControl == null || !_statsControl.IsDead;
    public Transform LockCenter => _lockCenter;
    public CharacterStatsControl StatsControl => _statsControl;
    /// <summary>角色表现配置：受击/待机等表现使用的 VFX 与音频 key，由子类 prefab 挂对应 SO_*Presentation 资产。</summary>
    public SO_CharacterPresentation Presentation => _presentation;

    public VFXService VFXService => _vfxService;
    public AudioService AudioService => _audioService;

    protected virtual void Awake()
    {
        _vfxService = Services.Get<VFXService>();
        _audioService = Services.Get<AudioService>();
        _effectService = Services.Get<EffectService>();
    }
    protected virtual void Update() { }

    /// <summary>受击钩子：由 NetworkHub 在 Host 侧经目标 NetworkObjectId 定位后调用，统一经 EffectControl 施加 effect（查表深拷贝 + TempEffectSource 包装 multiplier）。
    /// 子类重写应先播受击动画/计数再 base 施加。contactPoint 供受击态按位置选方向动画。isStun_=true 表示击晕型攻击（由玩家子类拦截进击晕态，非受击态）。</summary>
    public virtual void OnHitReceived(string effectId_, float multiplier_, Vector3 contactPoint_, bool isStun_ = false)
    {
        var effect = _effectService.GetEffect(effectId_);
        if (effect == null) return;
        var source = new TempEffectSource(multiplier_);
        _statsControl.EffectControl.ApplyEffect(effect, _statsControl, source);
    }
}

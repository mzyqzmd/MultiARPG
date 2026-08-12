using Sirenix.OdinInspector;
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

    // 服务
    protected EffectService _effectService;
    protected VFXService _vfxService;

    public Animator Anim => _anim;
    // ILockableTarget
    /// <summary>是否可被锁定：死亡（本地血量归零）时自动不可锁定，复活后自动恢复。</summary>
    public virtual bool CanBeLocked => _statsControl == null || !_statsControl.IsDead;
    public Transform LockCenter => _lockCenter;
    public CharacterStatsControl StatsControl => _statsControl;

    public EffectService EffectService => _effectService;
    public VFXService VFXService => _vfxService;

    protected virtual void Awake()
    {
        _effectService = Services.Get<EffectService>();
        _vfxService = Services.Get<VFXService>();
    }
    protected virtual void Update() { }

    #region 受击入口（命中链路统一钩子：扣血 + 进受击态）
    /// <summary>受击钩子：由 NetworkHub 在 Host 侧经目标 NetworkObjectId 定位后调用，统一驱动扣血（基类默认）与进受击态（子类重写先播动画再 base 扣血）。contactPoint 供受击态按位置选方向动画。</summary>
    public virtual void OnHitReceived(DamageInfo damage_, Vector3 contactPoint_) =>
        _statsControl.TakeDamage(damage_);
    #endregion

    #region 效果施加入口
    /// <summary>自身/环境效果本地施加入口：owner 端在本地应用（陷阱 / 环境伤害，无攻击者来源）。本地权威下不绕 server。</summary>
    public void ApplyEffect(string effectId_)
    {
        if (!IsOwner) return;
        var effect = _effectService.GetEffect(effectId_);
        if (effect != null) _statsControl.ApplyEffect(effect, null);
    }

    /// <summary>效果移除本地入口：owner 端按 id 移除已施加效果（与 ApplyEffect 对称）。</summary>
    public void RemoveEffect(string effectId_)
    {
        if (!IsOwner) return;
        _statsControl?.RemoveEffect(effectId_);
    }
    #endregion
}

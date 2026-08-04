using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 角色基类（NetworkBehaviour）：组合 CharacterStatsControl 与 CharacterNetworkCombatControl。
/// 网络外观层——StatsControl 数值本地权威（owner 端应用与驱动），本类仅负责：
/// 自身/环境效果本地施加入口、受击扣血钩子。
/// 自身不再实现 IEffectTarget（效果目标/来源已下沉到 CharacterStatsControl）。
/// _statsControl 由子类在 Awake 中赋值（如 Player 赋 PlayerStatsControl）。
/// </summary>
public abstract class Character : NetworkBehaviour
{
    [Header("Character基础配置")]
    [SerializeField] protected CharacterController _characterController;
    [SerializeField] protected Animator _anim;

    // 服务
    protected EffectService _effectService;
    protected VFXService _vfxService;

    // 属性控制
    protected CharacterStatsControl _statsControl;// 本地属性控制（子类 Awake 赋值），同时是 IEffectTarget / IEffectSource

    public CharacterStatsControl StatsControl => _statsControl;

    protected virtual void Awake()
    {
        _effectService = Services.Get<EffectService>();
        _vfxService= Services.Get<VFXService>();
    }
    protected virtual void Update()
    {
        // 持续型效果（Timed / Static）由各 owner 端本地驱动（本地权威：数值修改权在 owner client）
        if (IsOwner) _statsControl?.TickEffects(Time.deltaTime);
    }

    #region 本地血量变化订阅（驱动回调，如复活/死亡态由子类 FSM 本地判定）
    public override void OnNetworkSpawn()
    {
        _statsControl.OnCurrentHealthChanged += OnCurrentHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        _statsControl.OnCurrentHealthChanged -= OnCurrentHealthChanged;
    }

    /// <summary>本地血量变化回调钩子（子类可重写；基类默认空实现）。</summary>
    protected virtual void OnCurrentHealthChanged(int _, int new_) { }
    #endregion

    #region 受击入口（命中链路统一钩子：扣血 + 进受击态）
    /// <summary>受击钩子：由 NetworkHub 在 Host 侧经目标 NetworkObjectId 定位后调用，统一驱动扣血（基类默认）与进受击态（子类重写先播动画再 base 扣血）。contactPoint 供受击态按位置选方向动画。</summary>
    public virtual void OnHitReceived(DamageInfo damage_, Vector3 contactPoint_) =>
        _statsControl?.TakeDamage(damage_);
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

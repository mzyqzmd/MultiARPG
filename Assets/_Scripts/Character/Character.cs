using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 角色基类（NetworkBehaviour）：组合 CharacterStatsControl 与 CharacterNetworkStatsControl。
/// 网络外观层——StatsControl 数值本地权威（owner 端应用与驱动），本类仅负责：
/// 死亡上报（本地血量归零经 _networkStatsControl 上报 server 广播）、自身/环境效果本地施加入口、跨玩家伤害占位。
/// 自身不再实现 IEffectTarget（效果目标/来源已下沉到 CharacterStatsControl）。
/// _statsControl 由子类在 Awake 中赋值（如 Player 赋 PlayerStatsControl）。
/// </summary>
public abstract class Character : NetworkBehaviour
{
    [Header("Character基础配置")]
    [SerializeField] protected CharacterController _characterController;
    [SerializeField] protected Animator _anim;

    // 效果查表服务
    protected EffectService _effectService;

    // 属性控制
    protected CharacterStatsControl _statsControl;// 本地属性控制（子类 Awake 赋值），同时是 IEffectTarget / IEffectSource
    protected CharacterNetworkStatsControl _networkStatsControl;// 网络属性控制

    protected virtual void Awake()
    {
        _effectService = Services.Get<EffectService>();
    }
    protected virtual void Update()
    {
        // 持续型效果（Timed / Static）由各 owner 端本地驱动（本地权威：数值修改权在 owner client）
        if (IsOwner) _statsControl?.TickEffects(Time.deltaTime);
    }

    #region 死亡上报（本地血量归零 → 经网络状态组件上报 server 广播，使队友端进入死亡态）
    /// <summary>网络生命周期：server 复位死亡状态并订阅本地血量变化（死亡上报用）。</summary>
    public override void OnNetworkSpawn()
    {
        if (IsServer) _networkStatsControl.SetAlive();
        _statsControl.OnCurrentHealthChanged += OnCurrentHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (_statsControl != null)
            _statsControl.OnCurrentHealthChanged -= OnCurrentHealthChanged;
    }

    /// <summary>本地血量归零即上报死亡（_networkStats 为 null 的非联网角色静默忽略）。</summary>
    private void OnCurrentHealthChanged(int _, int new_)
    {
        if (new_ <= 0) _networkStatsControl.ReportDeath();
    }
    #endregion

    #region 效果施加入口
    /// <summary>自身/环境效果本地施加入口：owner 端在本地应用（陷阱 / 环境伤害，无攻击者来源）。本地权威下不绕 server。</summary>
    public void ApplyEffect(string effectId_)
    {
        if (!IsOwner) return;
        var effect = _effectService.GetEffect(effectId_);
        if (effect != null) _statsControl.ApplyEffect(effect, null);
    }

    /// <summary>跨玩家攻击结算入口：以自身 StatsControl 作为攻击者来源，用实时攻击属性直接构造 DamageInfo，经 IEffectTarget.TakeDamage 作用目标。
    /// 注意（本地权威模型·占位）：当前为直接改"攻击者机器上目标的本地副本"，对目标真实 owner 无效。
    /// 完整结算需战斗阶段补「攻击者 → Host 中继 → 目标 owner 本地 TakeDamage」RPC 链路（DamageInfo 跨端传递），本次不实现。</summary>
    public void DealDamage(IEffectTarget target_, Vector3 contactPoint_)
    {
        if (!IsServer) return;
        if (_statsControl == null || target_ == null) return;
        var dmg = new DamageInfo(_statsControl.PhysicalAttack, _statsControl.MagicAttack, _statsControl.FireAttack,
            _statsControl.IceAttack, _statsControl.LightningAttack, contactPoint_, _statsControl, target_);
        target_.TakeDamage(dmg);
    }
    #endregion
}

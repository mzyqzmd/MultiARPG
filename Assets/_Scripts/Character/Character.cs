using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 角色基类（NetworkBehaviour）：实现 IEffectTarget，组合 EffectControl。
/// 属性修正统一在此分流：本地属性直落本地 CharacterStatsControl（server 应用 + ClientRpc 广播各端本地应用）；
/// 生命值/上限为纯本地属性（仿耐力），server 施加后仅向 Owner 发定向 ClientRpc，其他玩家零感知。
/// _statsControl 由子类在 Awake 中赋值（如 Player 赋 PlayerStatsControl）。
/// </summary>
public abstract class Character : NetworkBehaviour, IEffectTarget
{
    [Header("Character基础配置")]
    [SerializeField] protected CharacterController _characterController;
    [SerializeField] protected Animator _anim;

    // 效果控制
    protected EffectService _effectService;
    protected EffectControl _effectControl;
    protected IEffectSource _effectSource;// 攻击者来源（子类 Awake 赋值，供攻击结算读取实时攻击属性）

    // 属性控制
    protected CharacterStatsControl _statsControl;// 本地属性控制（子类 Awake 赋值）
    protected CharacterNetworkStatsControl _networkStatsControl;// 网络属性控制

    protected virtual void Awake()
    {
        _effectService = Services.Get<EffectService>();
        _effectControl = new EffectControl();
    }
    protected virtual void Update()
    {
        // 持续型效果（Timed / Static）仅在 server 端权威驱动，生命变化经定向 ClientRpc 推送 Owner
        if (IsServer) _effectControl?.Tick(Time.deltaTime, this);
    }

    #region 死亡同步（接入网络状态组件）
    /// <summary>网络生命周期：取得同对象的网络状态组件并订阅本地血量变化；server 复位死亡状态（死亡不存档）。</summary>
    public override void OnNetworkSpawn()
    {
        if (IsServer) _networkStatsControl.SetAlive();
        _statsControl.OnCurrentHealthChanged += OnCurrentHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (_statsControl != null) _statsControl.OnCurrentHealthChanged -= OnCurrentHealthChanged;
    }

    /// <summary>本地血量归零即上报死亡（_networkStats 为 null 的非联网角色静默忽略）。</summary>
    private void OnCurrentHealthChanged(int _, int new_)
    {
        if (new_ <= 0) _networkStatsControl.ReportDeath();
    }
    #endregion

    #region 效果施加入口
    /// <summary>权威施加入口：任意客户端调用后由 server 端执行（server 取效果并施加到自身）。无攻击者来源（陷阱 / 环境伤害）。</summary>
    [ServerRpc]
    public void ApplyEffectServerRpc(string effectId_)
    {
        var effect = _effectService.GetEffect(effectId_);
        if (effect != null) _effectControl.ApplyEffect(effect, this, null);
    }

    /// <summary>IEffectTarget 施加入口：转发到自身 EffectControl 并带入攻击者来源（环境伤害 source_ 为 null）。</summary>
    public virtual void ApplyEffect(SO_BaseEffect effect_, IEffectSource source_) =>
        _effectControl.ApplyEffect(effect_, this, source_);

    /// <summary>服务端攻击结算入口：以自身为攻击者来源，用实时攻击属性直接构造 DamageInfo，经 IEffectTarget.TakeDamage 专用通道作用目标（纯原始属性，不做倍率；伤害数值运行时算出）。供战斗系统经 ServerRpc 调用。</summary>
    public void DealDamage(IEffectTarget target_, Vector3 contactPoint_)
    {
        if (!IsServer) return;
        if (_effectSource == null || target_ == null) return;
        var dmg = new DamageInfo(_effectSource.PhysicalAttack, _effectSource.MagicAttack, _effectSource.FireAttack,
            _effectSource.IceAttack, _effectSource.LightningAttack, contactPoint_, _effectSource, target_);
        target_.TakeDamage(dmg);
    }
    #endregion

    #region IEffectTarget
    /// <summary>受伤（server 权威）：本地 stats 扣血后仅向 Owner 定向推送当前生命值，其他玩家零感知。</summary>
    public virtual void TakeDamage(DamageInfo damage_)
    {
        _statsControl.TakeDamage(damage_.TotalDamage);
        RelayHealthToOwner();
    }

    /// <summary>治疗（server 权威）：本地 stats 加血后仅向 Owner 定向推送当前生命值，其他玩家零感知。</summary>
    public virtual void Heal(int amount_)
    {
        _statsControl.Heal(amount_);
        RelayHealthToOwner();
    }

    /// <summary>施加属性修正（server 权威）：本地属性落 server 本地 stats；MaxHealth 仅向 Owner 定向推送，其余属性全客户端广播。</summary>
    public virtual void ApplyStatModifier(EStatType type_, StatModifier modifier_)
    {
        if (!IsServer) return;
        _statsControl.AddModifier(type_, modifier_);
        if (type_ == EStatType.MaxHealth) RelayStatModifierToOwner(type_, modifier_, true);
        else BroadcastModifierClientRpc(type_, modifier_, true);
    }

    /// <summary>移除属性修正（server 权威）：与施加对称回滚。</summary>
    public virtual void RemoveStatModifier(EStatType type_, StatModifier modifier_)
    {
        if (!IsServer) return;
        _statsControl.RemoveModifier(type_, modifier_);
        if (type_ == EStatType.MaxHealth) RelayStatModifierToOwner(type_, modifier_, false);
        else BroadcastModifierClientRpc(type_, modifier_, false);
    }

    [ClientRpc]
    private void BroadcastModifierClientRpc(EStatType type_, StatModifier modifier_, bool isApply_)
    {
        // Host 已在 server 侧应用过本地修正，此处仅纯客户端应用
        if (IsServer) return;
        if (isApply_) _statsControl.AddModifier(type_, modifier_);
        else _statsControl.RemoveModifier(type_, modifier_);
    }

    /// <summary>仅 Owner 端用最新生命值覆盖本地 stats（其他玩家不接收）。</summary>
    [ClientRpc]
    private void ReceiveHealthClientRpc(int currentHealth_, ClientRpcParams _ = default)
    {
        if (IsServer) return;
        _statsControl.RestoreHealth(currentHealth_);
    }

    /// <summary>仅 Owner 端本地应用/回滚生命上限修正（其他玩家不接收）。</summary>
    [ClientRpc]
    private void ReceiveStatModifierClientRpc(EStatType type_, StatModifier modifier_, bool isApply_, ClientRpcParams _ = default)
    {
        if (IsServer) return;
        if (isApply_) _statsControl.AddModifier(type_, modifier_);
        else _statsControl.RemoveModifier(type_, modifier_);
    }

    /// <summary>向 Owner 定向推送当前生命值（Host 自身因 IsServer 守卫跳过，不重复应用）。</summary>
    private void RelayHealthToOwner()
    {
        var param = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { NetworkObject.OwnerClientId } }
        };
        ReceiveHealthClientRpc(_statsControl.CurrentHealth, param);
    }

    /// <summary>向 Owner 定向推送生命上限修正（Host 自身因 IsServer 守卫跳过，不重复应用）。</summary>
    private void RelayStatModifierToOwner(EStatType type_, StatModifier modifier_, bool isApply_)
    {
        var param = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { NetworkObject.OwnerClientId } }
        };
        ReceiveStatModifierClientRpc(type_, modifier_, isApply_, param);
    }
    #endregion
}

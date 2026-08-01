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

    public CharacterStatsControl StatsControl => _statsControl;

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

    /// <summary>效果移除本地入口：owner 端按 id 移除已施加效果（与 ApplyEffect 对称）。</summary>
    public void RemoveEffect(string effectId_)
    {
        if (!IsOwner) return;
        _statsControl?.RemoveEffect(effectId_);
    }
    #endregion
}

using UnityEngine;

/// <summary>
/// 轻剑（Rapier）接触伤害：仅攻击者本机结算命中（Owner.IsOwner 守卫），经 Host 中转把伤害发给目标 owner。
/// 已装备状态下碰撞体碰到 Damageable 目标即构造 DamageInfo 并发起 RequestDamageServerRpc（冷却 _meleeCooldown 防连触）；
/// 走路意外蹭到敌人也会掉血（用户明确要求）。未装备的孤儿武器走独立接触路径（无冷却），保持场景陷阱/掉落物行为。
/// 不读取任何 Player 攻击标志，状态类也不碰本武器字段。
/// </summary>
public class Weapon_Rapier : BaseItem
{
    [Tooltip("命中后伤害冷却（秒）：冷却内重复接触不再结算，避免单帧多次触发。")]
    [SerializeField] private float _meleeCooldown = 0.5f;

    /// <summary>当前蓄力倍率载体：由 PlayerHeavyAttackState 退出蓄力时按分级写入（太短=1、短蓄=ShortChargeMulti、满蓄=LongChargeMulti）；命中聚合后复位为 1 防残留。</summary>
    public float CurrentMultiplier { get; set; } = 1f;

    private VFXService _vfxService;
    private AudioService _audioService;
    private float _damageableInterval;

    private void Awake()
    {
        _vfxService = Services.Get<VFXService>();
        _audioService = Services.Get<AudioService>();
    }

    private void Update() =>
        _damageableInterval = Mathf.Max(0f, _damageableInterval - Time.deltaTime);

    private void OnTriggerEnter(Collider other)
    {
        if (!IsEquipped) return;
        //仅攻击者本机结算命中：消除多端重复结算（远端副本不触发，IsEquipped 是否同步无关紧要）。
        if (Owner == null || !Owner.IsOwner) return;

        var targetCharacter = other.gameObject.GetComponentInParent<Character>();
        if (targetCharacter == null) return;
        var target = other.GetComponentInParent<IEffectTarget>();
        var source = Owner.StatsControl;
        if (ReferenceEquals(target, source)) return;

        if (IsEquipped)
        {
            if (_damageableInterval > 0f) return;
            _damageableInterval = _meleeCooldown;
        }

        //从武器 effect 集合聚合伤害并交给目标角色经 Host 中转（目标 owner 本地扣血 + 其他端播受击表现）；倍率来自当前蓄力等级。
        var contactPoint = other.ClosestPoint(transform.position);
        var damage = DamageInfo.FromEffects(_itemData.Effects, contactPoint, source, target, CurrentMultiplier);
        //攻击者本机命中瞬间立即本地播受击表现（飙血 VFX + 受伤音效，零网络延迟）。
        _vfxService.PlayVFX("FX_BloodSplatter", contactPoint);
        _audioService.PlayAudio("Player", "BloodSplatter", contactPoint);
        _audioService.PlayAudio("Player", "Hit", contactPoint);
        //经通用中转站 NetworkHub 发 Host：定位目标并扣血，受击表现广播给除攻击者外的其他端（本机已本地播，避免重复）。
        NetworkHub.Instance.RequestDamageServerRpc(Owner.OwnerClientId, targetCharacter.NetworkObjectId, damage);
        //命中即消费倍率，复位为 1 防止泄漏到后续轻击/下次攻击。
        CurrentMultiplier = 1f;
    }

    /// <summary>重击/特殊使用入口（SpecialApply 键）。近战剑的蓄力重击由输入层 OnSpecialApply 直接驱动状态机，此处留空；远程武器(弓/枪)可在此发起射击逻辑。</summary>
    public override void SpecialApply(Player player_) { }
}


using UnityEngine;

/// <summary>
/// 轻剑（Rapier）接触伤害：仅攻击者本机结算命中（Owner.IsOwner 守卫），经 Host 中转把伤害发给目标 owner。
/// 已装备状态下碰撞体碰到 Damageable 目标即构造 DamageInfo 并发起 RequestDamageServerRpc（冷却 _meleeCooldown 防连触）；
/// 碰撞体仅在攻击命中窗口期由动画事件启用，窗口外触碰不结算（避免走路蹭到目标误伤）。
/// </summary>
public class Weapon_Rapier : BaseWeapon
{
    [Tooltip("命中后伤害冷却（秒）：冷却内重复接触不再结算，避免单帧多次触发。")]
    [SerializeField] private float _meleeCooldown = 0.5f;

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

    protected override void OnTriggerEnter(Collider other_)
    {
        base.OnTriggerEnter(other_);
        if (!IsEquipped) return;
        //攻击命中窗口外（碰撞体未启用）触碰直接跳过结算，避免走路蹭到目标误伤。
        if (!_attackCollider.enabled) return;
        //仅攻击者本机结算命中：消除多端重复结算（远端副本不触发，IsEquipped 是否同步无关紧要）。
        if (Owner == null || !Owner.IsOwner) return;
        var targetCharacter = other_.gameObject.GetComponentInParent<Character>();
        if (targetCharacter == null) return;
        var target = other_.GetComponentInParent<IEffectTarget>();
        var source = Owner.StatsControl;
        if (ReferenceEquals(target, source)) return;

        if (IsEquipped)
        {
            if (_damageableInterval > 0f) return;
            _damageableInterval = _meleeCooldown;
        }

        //从武器 effect 集合聚合伤害并交给目标角色经 Host 中转（目标 owner 本地扣血 + 其他端播受击表现）；倍率来自当前蓄力等级。
        var contactPoint = other_.ClosestPoint(transform.position);
        var damage = DamageInfo.FromEffects(_weaponData.Effects, contactPoint, source, target, CurrentMultiplier);
        //攻击者本机命中瞬间立即本地播受击表现（按目标角色表现配置取 key，飙血 VFX + 受伤音效，零网络延迟）。
        var p = targetCharacter.Presentation;
        _vfxService.PlayVFX(p.HitVFXKey, contactPoint);
        _audioService.PlayAudio(p.HitSplatterGroup, p.HitSplatterKey, contactPoint);
        _audioService.PlayAudio(p.HitAudioGroup, p.HitAudioKey, contactPoint);
        //经通用中转站 NetworkHub 发 Host：定位目标并扣血，受击表现广播给除攻击者外的其他端（本机已本地播，避免重复）。
        NetworkHub.Instance.RequestDamageServerRpc(Owner.OwnerClientId, targetCharacter.NetworkObjectId, damage);
        //命中即消费倍率，复位为 1 防止泄漏到后续轻击/下次攻击。
        CurrentMultiplier = 1f;
    }
}


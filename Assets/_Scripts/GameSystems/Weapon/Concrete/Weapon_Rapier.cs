using UnityEngine;

/// <summary>
/// 轻剑（Rapier）接触伤害：仅攻击者本机结算命中（Owner.IsOwner 守卫），经 Host 中转把伤害发给目标 owner。
/// 已装备状态下碰撞体碰到 Damageable 目标即读 effectId+攻击者 DamageMultiplier 经 Host 中转（冷却 _meleeCooldown 防连触）；
/// 碰撞体仅在攻击命中窗口期由动画事件启用，窗口外触碰不结算（避免走路蹭到目标误伤）。
/// </summary>
public class Weapon_Rapier : BaseWeapon
{
    [Tooltip("命中后伤害冷却（秒）：冷却内重复接触不再结算，避免单帧多次触发。")]
    [SerializeField] private float _meleeCooldown = 0.5f;

    private VFXService _vfxService;
    private AudioService _audioService;
    private DamageTextService _damageTextService;
    private float _damageableInterval;

    private void Awake()
    {
        _vfxService = Services.Get<VFXService>();
        _audioService = Services.Get<AudioService>();
        _damageTextService = Services.Get<DamageTextService>();
    }

    private void Update() =>
        _damageableInterval = Mathf.Max(0f, _damageableInterval - Time.deltaTime);

    protected void OnTriggerEnter(Collider other_)
    {
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

        if (_damageableInterval > 0f) return;
        _damageableInterval = _meleeCooldown;

        //攻击者本机命中瞬间立即本地播受击表现（按目标角色表现配置取 key，飙血 VFX + 受伤音效，零网络延迟）。
        var contactPoint = other_.ClosestPoint(transform.position);
        var p = targetCharacter.Presentation;
        _vfxService.PlayVFX(p.HitVFXKey, contactPoint);
        _audioService.PlayAudio(p.HitSplatterGroup, p.HitSplatterKey, contactPoint);
        _audioService.PlayAudio(p.HitAudioGroup, p.HitAudioKey, contactPoint);
        //经通用中转站 NetworkHub 发 Host：传 effectId+multiplier（攻击者 DamageMultiplierEffect 蓄力时写入的倍率），
        //由 Host 定位目标 owner 经 EffectControl.ApplyEffect 统一施加 effect（查表深拷贝→Execute→TakeDamage）。
        float multiplier = source.DamageMultiplier;
        string effectId = _weaponData.EffectIds.Count > 0 ? _weaponData.EffectIds[0] : null;
        if (string.IsNullOrEmpty(effectId))
        {
            Debug.LogWarning("[Weapon_Rapier] 武器 _effectIds 未配置，扣血链路中止。请在 SO_Weapon 资产填入 effect id。");
            return;
        }
        //经通用中转站 NetworkHub 发 Host 之前，先在本机弹出伤害数字（与 VFX / 音效同帧，零往返延迟）：
        //数值走 DamageEffect.Compute 同一口径，保证飘字数字与目标端实际扣血一致。
        _damageTextService.SpawnByEffect(effectId, multiplier, contactPoint);
        NetworkHub.Instance.RequestDamageServerRpc(Owner.OwnerClientId, targetCharacter.NetworkObjectId, effectId, multiplier, contactPoint, false);
    }
}


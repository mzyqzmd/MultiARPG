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

    private float _damageableInterval;

    private void Update() =>
        _damageableInterval = Mathf.Max(0f, _damageableInterval - Time.deltaTime);

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Damageable")) return;
        //仅攻击者本机结算命中：消除多端重复结算（远端副本不触发，IsEquipped 是否同步无关紧要）。
        if (Owner == null || !Owner.IsOwner) return;

        var targetCharacter = other.gameObject.GetComponentInParent<Character>();
        if (targetCharacter == null) return;
        var target = targetCharacter.StatsControl;
        var source = Owner.StatsControl;
        if (target == source) return;

        if (IsEquipped)
        {
            if (_damageableInterval > 0f) return;
            _damageableInterval = _meleeCooldown;
        }

        //从武器 effect 集合聚合伤害并交给目标角色经 Host 中转（目标 owner 本地扣血）。
        var damage = DamageInfo.FromEffects(_itemData.Effects, other.ClosestPoint(transform.position), source, target);
        targetCharacter.RequestDamageServerRpc(targetCharacter.NetworkObjectId, damage);
    }
}


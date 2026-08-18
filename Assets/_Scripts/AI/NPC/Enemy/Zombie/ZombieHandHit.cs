using UnityEngine;

/// <summary>
/// 僵尸手部攻击碰撞体转发脚本（普通 MonoBehaviour，挂 hand_l/hand_r 子物体）。
/// 手部子物体才是 Rigidbody+Trigger 的物理事件第一接收方，根 Zombie 脚本收不到子物体 trigger，
/// 故由本脚本接收 OnTriggerEnter 并经 _owner 回调根物体发起近战伤害；同时提供 Enable/Disable 供攻击窗口开关。
/// </summary>
public class ZombieHandHit : MonoBehaviour
{
    [SerializeField] private Zombie _owner;
    [SerializeField] private Collider _collider;
    [SerializeField] private float _attackInterval = 2f;

    private float _cooldownTimer;
    private bool _hasHitThisSwing;

    /// <summary>攻击窗口开启：启用手部碰撞体。</summary>
    public void Enable() => _collider.enabled = true;

    /// <summary>攻击窗口关闭：禁用手部碰撞体并复位本次挥击去重标志。</summary>
    public void Disable()
    {
        _collider.enabled = false;
        _hasHitThisSwing = false;
    }

    #region 冷却计时
    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }
    #endregion

    private void OnTriggerEnter(Collider other_)
    {
        if (!_owner.IsServer || _cooldownTimer > 0f) return;
        var target = other_.GetComponentInParent<Character>();
        if (target == null || target == _owner) return;
        if (_hasHitThisSwing) return;
        _hasHitThisSwing = true;
        _cooldownTimer = _attackInterval;
        Vector3 contactPoint = other_.ClosestPoint(_collider.transform.position);
        var damage = new DamageInfo(_owner.ZombieStatsControl.AttackDamage, 0, 0, 0, 0,
            contactPoint, _owner.StatsControl, target.StatsControl);
        NetworkHub.Instance.RequestDamageServerRpc(_owner.NetworkObjectId, target.NetworkObjectId, damage);
    }
}

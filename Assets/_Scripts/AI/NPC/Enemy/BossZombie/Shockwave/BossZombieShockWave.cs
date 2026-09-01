using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 僵尸 Ground 攻击专属冲击波（P2 仅 Ground 触发，其余 4 动作不触发）。
/// 由 Ground 动画末帧 Animation Event 在 Host 端 spawn：半径由小到大扩张，
/// 扩张过程中 sphere 检测命中玩家 → 经 NetworkHub 带 isStun_=true 触发击晕；
/// 到期（扩张完成）自销毁。仅 Host 端生成与判定。
/// 每个目标在一次冲击波内只命中一次（_hitTargets 去重），避免扩张期逐帧重复扣血秒杀玩家。
/// </summary>
public class BossZombieShockWave : MonoBehaviour
{
    [SerializeField] private float _maxRadius = 6f;
    [SerializeField] private float _expandDuration = 0.6f;
    [Tooltip("冲击波效果 id（对应 SO_EffectDatabase key，可带轻伤害；击晕由 isStun_ 标志决定）。")]
    [SerializeField] private string _shockwaveEffectId;
    [Tooltip("检测球体每帧步进细分（半径越大细分越高，避免高速穿漏）。")]
    [SerializeField] private float _detectStep = 0.5f;

    private static readonly Collider[] _buffer = new Collider[16];

    private readonly HashSet<Character> _hitTargets = new HashSet<Character>();

    private BossZombie _owner;
    private float _elapsed;
    private float _lastRadius;
    private Transform _center;

    /// <summary>Host 端初始化：绑定拥有者（取 NetworkObjectId 作为攻击者）+ 检测中心。</summary>
    public void Initialize(BossZombie owner_, Transform center_)
    {
        _owner = owner_;
        _center = center_;
        _elapsed = 0f;
        _lastRadius = 0f;
        _hitTargets.Clear();
        transform.position = center_.position;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (_owner == null || !_owner.IsServer) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _expandDuration);
        float radius = Mathf.Lerp(0f, _maxRadius, t);
        transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
        transform.position = _center.position;

        // 边缘环形检测：以"上一帧半径→当前半径"的薄壳做 sphere overlap，避免穿漏。
        float inner = Mathf.Max(0f, _lastRadius);
        float thickness = Mathf.Max(radius - inner, _detectStep);
        DetectAndStun(inner, inner + thickness);
        _lastRadius = radius;

        if (_elapsed >= _expandDuration) Destroy(gameObject);
    }

    /// <summary>在 [innerR, outerR] 壳层内检测玩家：命中则经 NetworkHub 带 isStun_ 触发击晕，同一目标每次冲击波仅命中一次。</summary>
    private void DetectAndStun(float innerR_, float outerR_)
    {
        int overlap = Physics.OverlapSphereNonAlloc(_center.position, outerR_, _buffer, -1, QueryTriggerInteraction.Collide);
        for (int i = 0; i < overlap; i++)
        {
            var target = _buffer[i].GetComponentInParent<Character>();
            if (target == null || target == _owner) continue;
            if (target is AINPC) continue;
            float dist = Vector3.Distance(_center.position, target.transform.position);
            if (dist < innerR_) continue;
            // 去重：扩张期目标会连续多帧落在壳层内，未去重会逐帧扣血直接秒杀。
            if (!_hitTargets.Add(target)) continue;
            NetworkHub.Instance.RequestDamageServerRpc(_owner.NetworkObjectId, target.NetworkObjectId,
                _shockwaveEffectId, 1f, target.transform.position, true);
        }
    }
}

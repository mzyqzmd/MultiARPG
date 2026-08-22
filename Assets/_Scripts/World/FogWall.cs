using UnityEngine;

/// <summary>
/// 场景 boss 房间雾墙（纯本地、无网络同步）。
/// 检测体(trigger，需足够厚覆盖入口) + 阻挡体(实体薄墙) 双碰撞体。
/// OnTriggerStay 按本地 owner 玩家相对 transform.forward 的实时位置单向放行：
/// 房外侧关阻挡体放行进房，房内侧开阻挡体挡住出房（transform.forward 指向房间内）。
/// 不锁死：玩家房外复活重新接近仍可进；进房后出房仍被挡。Deactivate(boss 死亡) 全开双向自由。
/// 物理前提：本物体需 Rigidbody(IsKinematic) 才能令纯 trigger 对 CharacterController 触发 OnTrigger*。
/// </summary>
public class FogWall : MonoBehaviour
{
    [SerializeField] private BoxCollider _blockCollider;    // 阻挡用实体碰撞体，薄墙
    [SerializeField] private ParticleSystem _particleSystem;

    private Player _cachedPlayer;
    private float _cachedRadius;

    private void Start() => SetBlock(false);
    #region 被动受控接口（外部 Boss 房间控制器在 boss 死亡时调 Deactivate 全量放行）
    /// <summary>放行：boss 死亡统一出口。阻挡体变 trigger，双向自由通过。</summary>
    public void SetBlock(bool block_)
    {
        _blockCollider.isTrigger = !block_;   // 放行→变 trigger 穿过；挡→变实体挡住。尺寸/登记始终不变，避免首次卡顿
        if (_particleSystem.isPlaying == block_) return;
        if (block_) _particleSystem.Play();
        else _particleSystem.Stop();
    }
    #endregion

    #region 单向判定（OnTrigger* 仅用于发现/清除本地 owner 玩家，判定在 Update 每帧实时刷新）
    private void OnTriggerEnter(Collider other_)
    {
        var player = other_.GetComponentInParent<Player>();
        if (player == null || !player.IsOwner) return;
        _cachedPlayer = player;
        var cc = player.GetComponent<CharacterController>();
        _cachedRadius = cc.radius;
    }

    private void Update()
    {
        if (_cachedPlayer == null) return;
        var dotPos = Vector3.Dot(_cachedPlayer.transform.position - transform.position, transform.forward);
        // 中心在房外侧即放行（提前关墙避免擦碰），完全跨入房内才挡，过渡带保持上态防抖
        float threshold = _blockCollider.size.z * 0.5f + _cachedRadius;
        if (dotPos < 0f)
            SetBlock(false);
        else if (dotPos > threshold)
            SetBlock(true);
    }

    private void OnTriggerExit(Collider other_)
    {
        if (other_.GetComponentInParent<Player>() == _cachedPlayer)
        {
            _cachedPlayer = null;
            _cachedRadius = 0f;
        }
    }
    #endregion
}

using UnityEngine;

/// <summary>
/// 独立视觉检测：距离 + Linecast 遮挡 + 视野角度（敌人自身 forward 与玩家方向的夹角）。
/// 不与玩家 PlayerLockOnTarget 共享代码，封装为可被行为树 Condition 节点调用的纯判定组件。
/// </summary>
public class AIVision : MonoBehaviour
{
    [Header("视觉参数")]
    [SerializeField] private Transform _LookStartPoint;
    [SerializeField] private float _viewDistance = 12f;
    [SerializeField] private float _viewAngle = 110f;
    [SerializeField] private LayerMask _obstacleMask;

    /// <summary>能否看见目标：距离内 + 未被遮挡 + 在视野角度内。</summary>
    public bool CanSeePlayer(Transform target_)
    {
        if (target_ == null) return false;
        var toTarget = target_.position - _LookStartPoint.position;
        float dist = toTarget.magnitude;
        if (dist > _viewDistance) return false;

        var dir = toTarget / dist;
        float angle = Vector3.Angle(_LookStartPoint.forward, dir);
        if (angle > _viewAngle * 0.5f) return false;

        if (_obstacleMask != 0 &&
            Physics.Linecast(_LookStartPoint.position, target_.position, _obstacleMask))
            return false;

        return true;
    }
}

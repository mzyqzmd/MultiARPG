using UnityEngine;

/// <summary>
/// 可锁定目标接口：实现方需提供"是否允许被锁定"的动态开关与"锁定中心点"(用于相机 LookAt 与遮挡/距离检测)。
/// 不耦合具体层（玩家层/敌人层各自实现即可），锁定组件仅依赖此接口。
/// </summary>
public interface ILockableTarget
{
    /// <summary>是否允许被锁定（可动态开关，如死亡/隐身时返回 false）。</summary>
    bool CanBeLocked { get; }

    /// <summary>锁定中心点（世界坐标 Transform，通常为目标躯干/胸口的挂点）。</summary>
    Transform LockCenter { get; }
}

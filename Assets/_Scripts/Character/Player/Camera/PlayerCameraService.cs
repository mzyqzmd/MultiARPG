using UnityEngine;

public class PlayerCameraService : IService
{
    private Camera _cachedCamera;

    /// <summary>当前锁定目标（由 PlayerLockOnTargetComponent 在 Lock/Unlock 时写入，null=未锁定），供状态层将玩家朝向严格对准目标。</summary>
    public ILockableTarget CurrentLockTarget { get; set; }

    #region 对外暴露能力

    public Camera PlayerCamera
    {
        get
        {
            if (_cachedCamera == null)
                _cachedCamera = Camera.main;
            return _cachedCamera;
        }
    }

    public Vector3 MoveForward => Horizontalize(PlayerCamera.transform.forward);
    public Vector3 MoveRight => Horizontalize(PlayerCamera.transform.right);

    #endregion

    private Vector3 Horizontalize(Vector3 direction_)
    {
        direction_.y = 0f;
        return direction_.normalized;
    }
}

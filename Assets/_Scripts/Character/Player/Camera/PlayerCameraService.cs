using UnityEngine;

public class PlayerCameraService : IService
{
    private Camera _cachedCamera;

    public Camera PlayerCamera
    {
        get
        {
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            return _cachedCamera;
        }
    }
}

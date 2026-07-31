using Cinemachine;
using UnityEngine;

/// <summary>
/// 在 CinemachineInputProvider 基础上增加设备区分的输入增益。
/// 鼠标 delta 与手柄摇杆量级差异大，按当前活跃设备切换 Gain，避免手柄视角过慢。
/// </summary>
public class PlayerCameraInputProvider : CinemachineInputProvider
{
    [SerializeField] private Vector2 _mouseGain = Vector2.one;
    [SerializeField] private Vector2 _gamepadGain = new(2.5f, 8f);

    private InputService _inputService;
    private Vector2 _gain = Vector2.one;

    private void Start()
    {
        _inputService = Services.Get<InputService>();
        ApplyGain(_inputService.ActiveDevice);
        _inputService.OnActiveDeviceChanged += OnActiveDeviceChanged;
    }

    private void OnDestroy()
    {
        if (_inputService != null)
            _inputService.OnActiveDeviceChanged -= OnActiveDeviceChanged;
    }

    private void OnActiveDeviceChanged(EActiveDevice device_) => ApplyGain(device_);

    private void ApplyGain(EActiveDevice device_) =>
        _gain = device_ == EActiveDevice.Gamepad ? _gamepadGain : _mouseGain;

    public override float GetAxisValue(int axis)
    {
        float value = base.GetAxisValue(axis);
        if (axis == 0) return value * _gain.x;
        if (axis == 1) return value * _gain.y;
        return value;
    }
}

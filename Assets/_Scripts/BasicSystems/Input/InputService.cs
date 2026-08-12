using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputService : IService, GameInputControl.IUIActions, GameInputControl.IPlayerActions
{
    private readonly GameInputControl _control;
    private EActiveDevice _activeDevice;
    private bool _runHeld;

    public event Action OnSelectedButtonClicked;

    /// <summary>手柄摇杆幅度阈值（0~1）：超过即视为跑步，值越大需推得越满。</summary>
    private const float GamepadRunThreshold = 0.7f;
    /// <summary>闪避触发事件（performed 时触发一次）。</summary>
    public event Action OnDodgeTriggered;
    /// <summary>跳跃触发事件（performed 时触发一次）。</summary>
    public event Action OnJumpTriggered;
    /// <summary>测试按钮点击事件（performed 时触发一次）。</summary>
    public event Action OnTestButtonClicked;//NOTE:测试用的到时候删掉
    /// <summary>热键栏触发：参数为对应装备槽位（1=LeftHand / 2=LeftHip / 3=Back / 4=RightHip / 5=RightHand）。</summary>
    public event Action<EEquipmentSlot> OnHotbarTriggered;
    /// <summary>轻击触发事件（performed 时触发一次）。</summary>
    public event Action OnNormalAttackTriggered;
    /// <summary>重击开始事件（按键按下 started 相位触发，用于进入蓄力态并开始计时）。</summary>
    public event Action OnSpecialAttackTriggered;
    /// <summary>重击取消/松开事件（按键 released/canceled 相位触发，用于按蓄力时长结算并重击）。</summary>
    public event Action OnSpecialAttackCanceled;
    /// <summary>瞄准/格挡触发事件（performed 时触发一次，本次仅暴露不接线）。</summary>
    public event Action OnAimOrParryTriggered;
    /// <summary>锁定目标触发事件（performed 时触发一次）。</summary>
    public event Action OnLockOnTargetTriggered;
    /// <summary>活跃设备切换事件（键盘 ↔ 手柄）。</summary>
    public event Action<EActiveDevice> OnActiveDeviceChanged;

    /// <summary>当前活跃输入设备。</summary>
    public EActiveDevice ActiveDevice => _activeDevice;
    /// <summary>当前输入向量（键盘 ↔ 手柄）。</summary>
    public Vector2 MoveInputVector2 { get; private set; }

    /// <summary>当前是否在跑步：手柄按摇杆幅度、键盘按 RunTrigger 按键态，避免两套逻辑外泄给订阅方。</summary>
    public bool IsRunning
    {
        get
        {
            if (_activeDevice == EActiveDevice.Gamepad)
                return MoveInputVector2.magnitude >= GamepadRunThreshold;
            return _runHeld;
        }
    }
    public InputService()
    {
        _control = new GameInputControl();
        _activeDevice = InputSystem.GetDevice<Gamepad>() != null ? EActiveDevice.Gamepad : EActiveDevice.Keyboard;
        _control.Player.AddCallbacks(this);
        _control.UI.AddCallbacks(this);
    }

    public void Dispose() => _control.Dispose();

    #region 输入模式启用/禁用
    // 切换到玩家操作模式：禁用 UI 映射、启用 Player 映射
    public void EnablePlayerMode()
    {
        _control.UI.Disable();
        _control.Player.Enable();
    }

    // 切换到 UI 操作模式：禁用 Player 映射、启用 UI 映射
    public void EnableUIMode()
    {
        _control.Player.Disable();
        _control.UI.Enable();
    }
    public void DisableAllMode() => _control.Disable();
    #endregion

    // 根据触发来源设备更新活跃设备，切换时广播 OnActiveDeviceChanged
    private void UpdateActiveDevice(InputAction.CallbackContext context_)
    {
        EActiveDevice next = context_.control?.device is Gamepad ?
            EActiveDevice.Gamepad : EActiveDevice.Keyboard;
        if (next == _activeDevice) return;
        _activeDevice = next;
        OnActiveDeviceChanged?.Invoke(next);
    }

    #region UI动作触发：确认键按下（performed）时触发一次
    public void OnClickSelectedButton(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnSelectedButtonClicked?.Invoke();
    }
    #endregion

    #region Movement 动作触发：缓存移动向量（动作态直接读取 MoveInputVector2）
    public void OnMovement(InputAction.CallbackContext context_)
    {
        UpdateActiveDevice(context_);
        MoveInputVector2 = context_.ReadValue<Vector2>();
    }
    #endregion

    #region RunTrigger 动作触发（仅键盘绑定 leftShift）：更新按键态，并在移动中立即刷新跑动状态
    public void OnRunTrigger(InputAction.CallbackContext context_)
    {
        UpdateActiveDevice(context_);
        _runHeld = context_.ReadValueAsButton();
    }
    #endregion


    public void OnLook(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
    }

    public void OnDodgeTrigger(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnDodgeTriggered?.Invoke();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnJumpTriggered?.Invoke();
    }

    #region 装备栏热键触发
    public void OnEquipHotKey1(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnHotbarTriggered?.Invoke(EEquipmentSlot.LeftHand);
    }

    public void OnEquipHotKey2(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnHotbarTriggered?.Invoke(EEquipmentSlot.LeftHip);
    }

    public void OnEquipHotKey3(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnHotbarTriggered?.Invoke(EEquipmentSlot.Back);
    }

    public void OnEquipHotKey4(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnHotbarTriggered?.Invoke(EEquipmentSlot.RightHip);
    }

    public void OnEquipHotKey5(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnHotbarTriggered?.Invoke(EEquipmentSlot.RightHand);
    }
    #endregion

    public void OnTestButton(InputAction.CallbackContext context)
    {
        if (context.performed) OnTestButtonClicked?.Invoke();
    }

    public void OnNormalAttack(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnNormalAttackTriggered?.Invoke();
    }

    public void OnSpecialAttack(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.started) OnSpecialAttackTriggered?.Invoke();
        else if (context.canceled) OnSpecialAttackCanceled?.Invoke();
    }

    public void OnAimOrParry(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnAimOrParryTriggered?.Invoke();
    }
    public void OnLockOnTarget(InputAction.CallbackContext context)
    {
        UpdateActiveDevice(context);
        if (context.performed) OnLockOnTargetTriggered?.Invoke();
    }
}

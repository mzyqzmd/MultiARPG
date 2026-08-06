using System;
using UnityEngine;

/// <summary>
/// 玩家状态机编排：父层(姿态)=Free/Combat，子层(动作)=Movement/Dodge/Jump/Fall。
/// 子层在父层内始终运行；Dodge/Jump 用 High 优先级 AnyTransition 抢断，天然适用于两种姿态。
/// 移动/动画/输入/耐力/垂直运动依赖由 Player 经构造函数注入。
/// </summary>
public class PlayerStateControl
{
    private readonly StateMachine<PlayerStanceState> _fsm;
    private readonly StateMachine<PlayerActionState> _childFsm;
    private readonly InputService _inputService;
    private readonly PlayerStatsControl _playerStats;
    private readonly PlayerVerticalMotionControl _verticalMotion;
    private readonly PlayerEquipmentComponent _equipmentControl;
    private readonly CharacterController _characterController;

    private bool _dodgeRequested;
    private bool _jumpRequested;
    private bool _normalAttackRequested;
    private bool _hitRequested;

    /// <summary>待消费受击方向：0=前 1=后 2=左 3=右（基于受击接触点相对玩家本地空间计算，EnterHitState 写入、PlayerHitState.Enter 消费）。</summary>
    private int _pendingHitDirection;

    public PlayerMovementState MovementState { get; }
    public PlayerDodgeState DodgeState { get; }
    public PlayerJumpState JumpState { get; }
    public PlayerFallState FallState { get; }
    public PlayerDeadState DeadState { get; }
    public PlayerNormalAttackState NormalAttackState { get; }
    public PlayerHitState HitState { get; }

    /// <summary>战斗姿态锁定标志：true=进入战斗(八向strafe)，false=自由。后续绑定输入/锁定目标。</summary>
    public bool CombatLocked { get; set; }
    /// <summary>直接转发 CombatLocked 意图标志。</summary>
    public bool IsInCombat => CombatLocked;
    /// <summary>真实地面检测结果(由 GroundCheck 经 VerticalMotion 透传，供动作层每帧同步动画参数)。</summary>
    public bool IsGrounded => _verticalMotion.IsGrounded;
    /// <summary>是否正在跑步(供 Player 编排耐力消耗)。</summary>
    public bool IsRunning => _inputService.IsRunning;
    /// <summary>闪避一次性耐力消耗。</summary>
    public int DodgeStaminaCost => _playerStats.DodgeStaminaCost;
    /// <summary>主动跳跃一次性耐力消耗。</summary>
    public int JumpStaminaCost => _playerStats.JumpStaminaCost;
    /// <summary>轻击一次性耐力消耗。</summary>
    public int NormalAttackStaminaCost => _playerStats.NormalAttackStaminaCost;

    /// <summary>是否正处于不可被新一次性动作打断的动作中(Dodge/Jump/Fall/Draw 等)。各动作态 Enter 置 true、Exit 置 false；死亡态由 _playerStats.IsDead 判定，不在此维护。</summary>
    public bool IsBusy { get; set; }

    /// <summary>动作破锁请求事件：Dodge/Jump 进入时触发，由 Player 桥接 _lockOnComponent.ForceUnlock。状态机零反向依赖锁定组件，仅发信号。</summary>
    public event Action OnLockBreakRequested;
    /// <summary>请求解除锁定（进入 Dodge/Jump 时调用，触发 OnLockBreakRequested 事件）。</summary>
    public void RequestLockBreak() => OnLockBreakRequested?.Invoke();

    public PlayerStateControl(InputService inputService_,
        PlayerCameraService playerCameraService_, CharacterController characterController_,
        PlayerStatsControl playerStats_, Animator anim_, AudioService audioService_,
        UIService uiService_, VFXService vfxService_, PlayerVerticalMotionControl verticalMotionControl_,
        PlayerEquipmentComponent equipmentControl_)
    {
        _inputService = inputService_;
        _playerStats = playerStats_;
        _verticalMotion = verticalMotionControl_;
        _equipmentControl = equipmentControl_;
        _characterController = characterController_;

        _fsm = new StateMachine<PlayerStanceState>();
        var free = new PlayerFreeState(_fsm, this, anim_);
        var combat = new PlayerCombatState(_fsm, this, anim_);
        _fsm.Initialize(free);

        // 子状态机先建引用，再注入初始态实例
        _childFsm = _fsm.SetChildFsm<PlayerActionState>();
        MovementState = new PlayerMovementState(_childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_,
            uiService_, vfxService_, _verticalMotion);
        DodgeState = new PlayerDodgeState(_childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_,
            uiService_, vfxService_);
        JumpState = new PlayerJumpState(_childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_,
            uiService_, vfxService_, _verticalMotion);
        FallState = new PlayerFallState(_childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_,
            uiService_, vfxService_, _verticalMotion);
        DeadState = new PlayerDeadState(_childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_,
            uiService_, vfxService_);
        NormalAttackState = new PlayerNormalAttackState(_childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_,
            uiService_, vfxService_);
        HitState = new PlayerHitState(_childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_,
            uiService_, vfxService_);
        _childFsm.Initialize(MovementState);

        // 父层：进入/退出战斗姿态
        _fsm.AddTransition(free, combat, () => CombatLocked, ETransitionPriority.Normal);
        _fsm.AddTransition(combat, free, () => !CombatLocked, ETransitionPriority.Normal);

        // 子层：死亡为 High 优先级任意态抢断，注册先于 Dodge/Jump 使其先命中、可打断一切动作(含 mid-dodge 死亡)
        _childFsm.AddAnyTransition(DeadState,
            () => _playerStats.IsDead && !IsBusy, ETransitionPriority.High);
        // 受击为 High 优先级任意态抢断；进行中不可再次进入(互斥)；死亡态下禁止触发
        _childFsm.AddAnyTransition(HitState,
            () => _hitRequested && !IsBusy && !_playerStats.IsDead, ETransitionPriority.High);
        // 闪避/跳跃为 High 优先级任意态抢断；进行中不可再次进入(互斥)；死亡态下禁止触发
        // 注册顺序 Dodge 先于 Jump，使 Dodge 优先级略高
        _childFsm.AddAnyTransition(DodgeState,
            () => _dodgeRequested && !IsBusy && !_playerStats.IsDead, ETransitionPriority.High);
        _childFsm.AddAnyTransition(JumpState,
            () => _jumpRequested && !IsBusy && _verticalMotion.CanJump && !_playerStats.IsDead,
            ETransitionPriority.High);
        // 轻击为 High 优先级任意态抢断；进行中不可再次进入(互斥)；死亡态/无右手装备禁用；任意姿态(Free/Combat)均可触发
        _childFsm.AddAnyTransition(NormalAttackState,
            () => _normalAttackRequested && !IsBusy && !_playerStats.IsDead &&
                  _equipmentControl.GetEquippedItem(EEquipmentSlot.RightHand) != null,
            ETransitionPriority.High);

        _inputService.OnDodgeTriggered += () => _dodgeRequested = true;
        _inputService.OnJumpTriggered += () => _jumpRequested = true;
        _inputService.OnNormalApplyTriggered += () => _normalAttackRequested = true;
    }

    /// <summary>消费闪避请求(进入 Dodge 态时调用，避免重复触发)。</summary>
    public void ConsumeDodge() => _dodgeRequested = false;
    /// <summary>消费轻击请求(进入 NormalAttack 态时调用，避免重复触发)。</summary>
    public void ConsumeNormalAttack() => _normalAttackRequested = false;
    /// <summary>消费跳跃请求(进入 Jump 态时调用，避免重复触发)。</summary>
    public void ConsumeJump() => _jumpRequested = false;
    /// <summary>消费受击请求(进入 Hit 态时调用，避免重复触发)。</summary>
    public void ConsumeHit() => _hitRequested = false;
    /// <summary>待消费受击方向(EnterHitState 写入、PlayerHitState.Enter 读取，用于选方向动画)。</summary>
    public int PendingHitDirection => _pendingHitDirection;

    /// <summary>受击入口：按接触点相对玩家本地空间算出前/后/左/右方向并缓存，置标志后切入受击态（不向状态传参，方向经 PendingHitDirection 在 Enter 时消费）。</summary>
    public void EnterHitState(Vector3 contactPoint_)
    {
        var local = _characterController.transform.InverseTransformDirection(
            (contactPoint_ - _characterController.transform.position).normalized);
        _pendingHitDirection = local.z >= Mathf.Abs(local.x) ? (local.z >= 0 ? 0 : 1)
            : (local.x >= 0 ? 3 : 2);
        // 仅置标志：HitState 的 AnyTransition（_hitRequested && !IsBusy && !IsDead）由下一帧 EvaluateTransitions 自动抢断进入，
        // 与 Dodge/Jump/NormalAttack 的纯标志驱动写法保持一致，避免手动切换与自动抢断重复触发。
        _hitRequested = true;
    }

    #region 每帧驱动(父层 Update 会级联驱动子层)
    public void Update()
    {
        // 死亡态下丢弃一次性动作请求，配合 !IsDead 守卫双保险，避免脏转换
        if (_playerStats.IsDead)
        {
            _jumpRequested = false;
            _dodgeRequested = false;
            _normalAttackRequested = false;
            _hitRequested = false;
        }
        // 进行中动作(不可中断)收到的新一次性动作请求直接丢弃，避免缓存到结束后自动再触发
        else if (IsBusy)
        {
            _jumpRequested = false;
            _dodgeRequested = false;
            _normalAttackRequested = false;
            _hitRequested = false;
        }
        // 耐力不足以支付闪避则丢弃请求
        if (_dodgeRequested && !_playerStats.CanAffordStamina(_playerStats.DodgeStaminaCost))
            _dodgeRequested = false;
        // 耐力不足以支付跳跃则丢弃请求
        if (_jumpRequested && !_playerStats.CanAffordStamina(_playerStats.JumpStaminaCost))
            _jumpRequested = false;
        // 耐力不足以支付轻击则丢弃请求
        if (_normalAttackRequested && !_playerStats.CanAffordStamina(_playerStats.NormalAttackStaminaCost))
            _normalAttackRequested = false;

        _fsm.Update();
    }
    #endregion
}

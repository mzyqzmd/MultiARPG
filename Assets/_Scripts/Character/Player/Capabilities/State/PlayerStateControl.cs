using UnityEngine;

/// <summary>
/// 玩家状态机编排：父层(姿态)=Free/Combat，子层(动作)=Movement/Dodge/Jump/Fall。
/// 子层在父层内始终运行；Dodge/Jump 用 High 优先级 AnyTransition 抢断，天然适用于两种姿态。
/// 移动/动画/输入/耐力/垂直运动依赖由 Player 经构造函数注入。
/// </summary>
public class PlayerStateControl
{
    private readonly StateMachine<PlayerStanceState> _fsm;
    private readonly InputService _inputService;
    private readonly PlayerStatsControl _playerStats;
    private readonly PlayerVerticalMotionControl _verticalMotion;

    private bool _dodgeRequested;
    private bool _jumpRequested;

    /// <summary>战斗姿态锁定标志：true=进入战斗(八向strafe)，false=自由。后续绑定输入/锁定目标。</summary>
    public bool CombatLocked { get; set; }

    public PlayerMovementState MovementState { get; }
    public PlayerDodgeState DodgeState { get; }
    public PlayerJumpState JumpState { get; }
    public PlayerFallState FallState { get; }
    public PlayerDeadState DeadState { get; }

    /// <summary>当前是否处于战斗姿态(由父层姿态状态显式声明，供动作层查询)。</summary>
    public bool IsInCombat => _fsm.CurrentState.IsInCombat;
    /// <summary>是否有待处理的闪避请求(供动作层 AnyTransition 判定)。</summary>
    public bool DodgeRequested => _dodgeRequested;
    /// <summary>是否有待处理的跳跃请求(供动作层 AnyTransition 判定)。</summary>
    public bool JumpRequested => _jumpRequested;
    /// <summary>当前是否正在闪避(由 Dodge 态维护，供 AnyTransition 防止进行中重复进入)。</summary>
    public bool IsDodging { get; set; }
    /// <summary>当前是否正在跳跃(由 Jump 态维护，供 AnyTransition 防止进行中重复进入)。</summary>
    public bool IsJumping { get; set; }
    /// <summary>当前是否正在坠落(由 Fall 态维护，供 AnyTransition 防止进行中重复进入)。</summary>
    public bool IsFalling { get; set; }
    /// <summary>当前是否已处于死亡态(由 Dead 态维护，供 AnyTransition 防止死亡持续态每帧自转移)。</summary>
    public bool IsDeadState { get; set; }
    /// <summary>是否正处于不可被新一次性动作打断的动作中(Dodge/Jump/Fall)。新增动作只需在此聚合处 || 自身标志。</summary>
    public bool IsBusy => IsDodging || IsJumping || IsFalling;
    /// <summary>真实地面检测结果(由 GroundCheck 经 VerticalMotion 透传，供动作层每帧同步动画参数)。</summary>
    public bool IsGrounded => _verticalMotion.IsGrounded;
    /// <summary>是否正在跑步(供 Player 编排耐力消耗)。</summary>
    public bool IsRunning => _inputService.IsRunning;
    /// <summary>闪避一次性耐力消耗。</summary>
    public int DodgeStaminaCost => _playerStats.DodgeStaminaCost;
    /// <summary>主动跳跃一次性耐力消耗。</summary>
    public int JumpStaminaCost => _playerStats.JumpStaminaCost;

    public PlayerStateControl(InputService inputService_,
        PlayerCameraService playerCameraService_, CharacterController characterController_,
        PlayerStatsControl playerStats_, Animator anim_, AudioService audioService_,
        UIService uiService_, PlayerVerticalMotionControl verticalMotionControl_)
    {
        _inputService = inputService_;
        _playerStats = playerStats_;
        _verticalMotion = verticalMotionControl_;

        _fsm = new StateMachine<PlayerStanceState>();
        var free = new PlayerFreeState(_fsm, this);
        var combat = new PlayerCombatState(_fsm, this);
        _fsm.Initialize(free);

        // 子状态机先建引用，再注入初始态实例
        var childFsm = _fsm.SetChildFsm<PlayerActionState>();
        MovementState = new PlayerMovementState(childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_, _verticalMotion);
        DodgeState = new PlayerDodgeState(childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_);
        JumpState = new PlayerJumpState(childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_, _verticalMotion);
        FallState = new PlayerFallState(childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_, _verticalMotion);
        DeadState = new PlayerDeadState(childFsm, this, characterController_,
            playerStats_, anim_, inputService_, playerCameraService_, audioService_, uiService_);
        childFsm.Initialize(MovementState);

        // 父层：进入/退出战斗姿态
        _fsm.AddTransition(free, combat, () => CombatLocked, ETransitionPriority.Normal);
        _fsm.AddTransition(combat, free, () => !CombatLocked, ETransitionPriority.Normal);

        // 子层：死亡为 High 优先级任意态抢断，注册先于 Dodge/Jump 使其先命中、可打断一切动作(含 mid-dodge 死亡)
        childFsm.AddAnyTransition(DeadState,
            () => _playerStats.IsDead && !IsDeadState, ETransitionPriority.High);
        // 闪避/跳跃为 High 优先级任意态抢断；进行中不可再次进入(互斥)；死亡态下禁止触发
        // 注册顺序 Dodge 先于 Jump，使 Dodge 优先级略高
        childFsm.AddAnyTransition(DodgeState,
            () => _dodgeRequested && !IsBusy && !_playerStats.IsDead, ETransitionPriority.High);
        childFsm.AddAnyTransition(JumpState,
            () => _jumpRequested && !IsBusy && _verticalMotion.CanJump && !_playerStats.IsDead,
            ETransitionPriority.High);
        // 复活(血量还原>0 且 server 复位死亡)后自动离开死亡态回移动
        childFsm.AddTransition(DeadState, MovementState, () => !_playerStats.IsDead, ETransitionPriority.Normal);

        _inputService.OnDodgeTriggered += () => _dodgeRequested = true;
        _inputService.OnJumpTriggered += () => _jumpRequested = true;
    }

    /// <summary>消费闪避请求(进入 Dodge 态时调用，避免重复触发)。</summary>
    public void ConsumeDodge() => _dodgeRequested = false;
    /// <summary>消费跳跃请求(进入 Jump 态时调用，避免重复触发)。</summary>
    public void ConsumeJump() => _jumpRequested = false;

    #region 每帧驱动(父层 Update 会级联驱动子层)
    public void Update()
    {
        // 死亡态下丢弃一次性动作请求，配合 !IsDead 守卫双保险，避免脏转换
        if (_playerStats.IsDead)
        {
            _jumpRequested = false;
            _dodgeRequested = false;
        }
        // 进行中动作(不可中断)收到的新一次性动作请求直接丢弃，避免缓存到结束后自动再触发
        else if (IsBusy)
        {
            _jumpRequested = false;
            _dodgeRequested = false;
        }
        // 耐力不足以支付闪避则丢弃请求
        if (_dodgeRequested && !_playerStats.CanAffordStamina(_playerStats.DodgeStaminaCost))
            _dodgeRequested = false;
        // 耐力不足以支付跳跃则丢弃请求
        if (_jumpRequested && !_playerStats.CanAffordStamina(_playerStats.JumpStaminaCost))
            _jumpRequested = false;

        _fsm.Update();
    }
    #endregion
}

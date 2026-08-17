using System;
using UnityEngine;

/// <summary>
/// 玩家状态机编排：父层(姿态)=Free/Combat，子层(动作)=Movement/Dodge/Jump/Fall。
/// 子层在父层内始终运行；Dodge/Jump 用 High 优先级 AnyTransition 抢断，天然适用于两种姿态。
/// 本类为 MonoBehaviour 组件（挂 Player.prefab 同对象）：服务类依赖在 Awake 经 Services 容器获取，
/// Component 类依赖经 Inspector [SerializeField] 注入，PlayerVerticalMotionControl 在 Awake 内部 new。
/// </summary>
public class PlayerStateControl : MonoBehaviour
{
    [Header("玩家引用")]
    [SerializeField] private Player _player;
    [Header("基础配置")]
    [SerializeField] private CharacterController _characterController;     // 角色控制器

    private PlayerVerticalMotionControl _verticalMotion;// 垂直运动
    private PlayerStateMachineBuilder _builder;   // 状态机构造器
    private PlayerStateAnimEventControl _stateAnimEventControl;

    #region 动作请求(Requested)
    public bool DodgeRequested => _dodgeRequested;
    public bool JumpRequested => _jumpRequested;
    public bool NormalAttackRequested => _normalAttackRequested;
    public bool HeavyAttackRequested => _heavyAttackRequested;
    public bool HitRequested => _hitRequested;

    private bool _dodgeRequested;              // 是否请求翻滚
    private bool _jumpRequested;               // 是否请求跳
    private bool _normalAttackRequested;       // 是否请求轻击
    private bool _heavyAttackRequested;        // 是否请求重击
    private bool _hitRequested;                // 是否请求受击 
    #endregion

    #region 玩家状态(_builder获取处实例)
    public PlayerMovementState MovementState => _builder.MovementState;
    public PlayerDodgeState DodgeState => _builder.DodgeState;
    public PlayerJumpState JumpState => _builder.JumpState;
    public PlayerFallState FallState => _builder.FallState;
    public PlayerDeadState DeadState => _builder.DeadState;
    public PlayerNormalAttackState NormalAttackState => _builder.NormalAttackState;
    public PlayerHeavyAttackState HeavyAttackState => _builder.HeavyAttackState;
    public PlayerHitState HitState => _builder.HitState;
    #endregion

    #region 是否处在某种状态
    /// <summary>直接转发 CombatLocked 意图标志。</summary>
    public bool IsInCombat => CombatLocked;
    /// <summary>真实地面检测结果(由 GroundCheck 经 VerticalMotion 透传，供动作层每帧同步动画参数)。</summary>
    public bool IsGrounded => _verticalMotion.IsGrounded;
    /// <summary>是否正在跑步(供 Player 编排耐力消耗)。</summary>
    public bool IsRunning => _player.InputService.IsRunning;
    /// <summary>是否正处于不可被新一次性动作打断的动作中(Dodge/Jump/Fall/Draw 等)。各动作态 Enter 置 true、Exit 置 false；死亡态由 _playerStats.IsDead 判定，不在此维护。</summary>
    public bool IsBusy { get; set; }
    #endregion

    #region 耐力消耗属性
    /// <summary>闪避一次性耐力消耗。</summary>
    public int DodgeStaminaCost => _player.StatsControl.DodgeStaminaCost;
    /// <summary>主动跳跃一次性耐力消耗。</summary>
    public int JumpStaminaCost => _player.StatsControl.JumpStaminaCost;
    /// <summary>轻击一次性耐力消耗。</summary>
    public int NormalAttackStaminaCost => _player.StatsControl.NormalAttackStaminaCost;
    /// <summary>重击(蓄力)一次性耐力消耗。</summary>
    public int HeavyAttackStaminaCost => _player.StatsControl.HeavyAttackStaminaCost;
    #endregion

    /// <summary>轻击连段窗口是否开启：仅窗口期内允许 AnyTransition 重入进下一段；窗口外按键不计入连段。</summary>
    public bool ComboWindowOpen { get; private set; }
    /// <summary>待消费受击方向(EnterHitState 写入、PlayerHitState.Enter 读取，用于 2D BlendTree 混合受击朝向；X=右/Y=前，范围[-1,1])。</summary>
    public float PendingHitX { get; private set; }
    /// <summary>待消费受击方向 Y 分量(前正后负)。</summary>
    public float PendingHitY { get; private set; }
    /// <summary>战斗姿态锁定标志：true=进入战斗(八向strafe)，false=自由。后续绑定输入/锁定目标。</summary>
    public bool CombatLocked { get; set; }

    /// <summary>动作破锁请求事件：Dodge/Jump 进入时触发，由 PlayerLockOnTarget 经 [SerializeField] 自订阅 ForceUnlock。状态机零反向依赖锁定组件，仅发信号。</summary>
    public event Action OnLockBreakRequested;

    /// <summary>请求解除锁定（进入 Dodge/Jump 时调用，触发 OnLockBreakRequested 事件）。</summary>
    public void RequestLockBreak() => OnLockBreakRequested?.Invoke();

    private void Awake()
    {
        //VerticalMotion组件构造
        _verticalMotion = new(_player.StatsControl, _characterController);
        // 状态机(两层 FSM + 8 状态 + 全部 AnyTransition)委托 Builder 构造，结果经本类只读属性透传
        _builder = new(_player, _characterController, _verticalMotion, this);
        _builder.Build();
        _stateAnimEventControl = new(_player, _builder);
    }

    #region Update
    public void Update()
    {
        // 死亡态下丢弃一次性动作请求，配合 !IsDead 守卫双保险，避免脏转换
        if (IsBusy || _player.StatsControl.IsDead)
        {
            _jumpRequested = false;
            _dodgeRequested = false;
            // 连段窗口未开启时清空轻击请求：窗口期外（含攻击前摇/后摇）的按键不计入连段，
            // 避免窗口关闭后残留请求等到下次窗口开启又误连；窗口开启期间保留供 AnyTransition 消费。
            if (!ComboWindowOpen)
                _normalAttackRequested = false;
            _heavyAttackRequested = false;
            _hitRequested = false;
        }

        // 耐力不足以支付闪避则丢弃请求
        if (_dodgeRequested && !_player.StatsControl.CanAffordStamina(_player.StatsControl.DodgeStaminaCost))
            _dodgeRequested = false;
        // 耐力不足以支付跳跃则丢弃请求
        if (_jumpRequested && !_player.StatsControl.CanAffordStamina(_player.StatsControl.JumpStaminaCost))
            _jumpRequested = false;
        // 耐力不足以支付轻击则丢弃请求
        if (_normalAttackRequested && !_player.StatsControl.CanAffordStamina(_player.StatsControl.NormalAttackStaminaCost))
            _normalAttackRequested = false;
        // 耐力不足以支付重击则丢弃请求
        if (_heavyAttackRequested && !_player.StatsControl.CanAffordStamina(_player.StatsControl.HeavyAttackStaminaCost))
            _heavyAttackRequested = false;

        // 非跑步期间(walk/站立/闪避/跳跃/坠落)经过延迟自动回复耐力
        if (!IsRunning)
            _player.StatsControl.RecoverStamina(Time.deltaTime);
        _builder.Fsm.Update();
    }
    #endregion

    #region 输入事件绑定（由 Player.OnNetworkSpawn/OnNetworkDespawn 在 IsOwner 可靠后转发调用）
    /// <summary>订阅本地玩家一次性动作输入事件（仅 owner 调用）。</summary>
    public void BindInput()
    {
        _player.InputService.OnDodgeTriggered += OnDodgeTriggered;
        _player.InputService.OnJumpTriggered += OnJumpTriggered;
        _player.InputService.OnNormalAttackTriggered += OnNormalAttackTriggered;
        _player.InputService.OnSpecialAttackTriggered += OnSpecialAttackTriggered;
        _player.InputService.OnSpecialAttackCanceled += OnSpecialAttackCanceled;
    }

    /// <summary>反订阅一次性动作输入事件（与 BindInput 配对，防重 spawn 累积）。</summary>
    public void UnbindInput()
    {
        _player.InputService.OnDodgeTriggered -= OnDodgeTriggered;
        _player.InputService.OnJumpTriggered -= OnJumpTriggered;
        _player.InputService.OnNormalAttackTriggered -= OnNormalAttackTriggered;
        _player.InputService.OnSpecialAttackTriggered -= OnSpecialAttackTriggered;
        _player.InputService.OnSpecialAttackCanceled -= OnSpecialAttackCanceled;
    }
    #endregion

    #region 绑定到输入服务的方法
    private void OnDodgeTriggered() => _dodgeRequested = true;
    private void OnJumpTriggered() => _jumpRequested = true;
    private void OnNormalAttackTriggered() => _normalAttackRequested = true;
    private void OnSpecialAttackTriggered()
    {
        _heavyAttackRequested = true;
        _player.Anim.SetBool("IsChargingHeavyAttack", true);
    }
    private void OnSpecialAttackCanceled()
    {
        _heavyAttackRequested = false;
        _player.Anim.SetBool("IsChargingHeavyAttack", false);
    }
    public void OnLockStateChange(bool isLocked_) => CombatLocked = isLocked_;
    #endregion

    #region 消费请求
    /// <summary>消费闪避请求(进入 Dodge 态时调用，避免重复触发)。</summary>
    public void ConsumeDodge() => _dodgeRequested = false;
    /// <summary>消费轻击请求(进入 NormalAttack 态时调用，避免重复触发)。</summary>
    public void ConsumeNormalAttack() => _normalAttackRequested = false;
    /// <summary>消费重击请求(进入 HeavyAttack 态时调用，避免重复触发)。</summary>
    public void ConsumeHeavyAttack() => _heavyAttackRequested = false;
    /// <summary>消费跳跃请求(进入 Jump 态时调用，避免重复触发)。</summary>
    public void ConsumeJump() => _jumpRequested = false;
    /// <summary>消费受击请求(进入 Hit 态时调用，避免重复触发)。</summary>
    public void ConsumeHit() => _hitRequested = false;
    #endregion

    #region 动画事件转发
    /// <summary>轻击连段窗口开启（动画事件 OnComboWindowOpen 驱动）：置标志，AnyTransition 此刻起允许消费轻击请求连段。</summary>
    public void OpenComboWindow() => ComboWindowOpen = true;
    /// <summary>轻击段结束（窗口关闭，动画事件 OnComboWindowClose 驱动）：关标志并清残留请求，避免窗口外误连。</summary>
    public void CloseComboWindow()
    {
        ComboWindowOpen = false;
        _normalAttackRequested = false;
    }
    /// <summary>动画事件驱动的蓄力结算：按 tier 写倍率到右手武器并扣耐力。tier>=2=满蓄(LongChargeMulti)，否则短蓄(ShortChargeMulti)。不在此切状态——倍率写完后由动画机 IsChargingHeavyAttack==false 自动流转释放动画，释放动画末尾 clip 事件再驱动退出。</summary>
    public void EndHeavyChargeByTier(int tier_) => _stateAnimEventControl.EndHeavyChargeByTier(tier_);
    public void ReturnToMovementState() => _stateAnimEventControl.ReturnToMovementState();
    #endregion

    #region 受击入口
    /// <summary>受击入口：按接触点相对玩家本地空间算出连续方向(X=右/Y=前,范围[-1,1])并缓存，置标志后切入受击态（不向状态传参，方向经 PendingHitX/Y 在 Enter 时消费驱动 2D BlendTree）。</summary>
    public void EnterHitState(Vector3 contactPoint_)
    {
        Vector3 local = _characterController.transform.InverseTransformDirection(
            contactPoint_ - _characterController.transform.position);
        Vector2 localVector2 = new Vector2(local.x, local.z).normalized;
        PendingHitX = localVector2.x;
        PendingHitY = localVector2.y;
        _hitRequested = true;
    } 
    #endregion
}

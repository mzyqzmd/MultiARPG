using System;
using UnityEngine;

/// <summary>
/// 玩家状态机构造器：普通类（非 MonoBehaviour），以构造函数注入依赖，承担两层状态机(父=姿态/子=动作)
/// 的 new 构造、父子 FSM 绑定与全部 AnyTransition 注册。构造结果以只读属性暴露，供 PlayerStateControl 透传引用。
/// 纯逻辑搬迁，不引入任何运行时行为变化。
/// </summary>
public class PlayerStateMachineBuilder
{
    private readonly Player _player;
    private readonly CharacterController _characterController;
    private readonly PlayerVerticalMotionControl _verticalMotion;
    private readonly PlayerStateControl _stateControl;

    public PlayerStateMachineBuilder(Player player_, CharacterController characterController_, PlayerVerticalMotionControl verticalMotion_, PlayerStateControl stateControl_)
    {
        _player = player_;
        _characterController = characterController_;
        _verticalMotion = verticalMotion_;
        _stateControl = stateControl_;
    }

    public StateMachine<PlayerStanceState> Fsm { get; private set; }
    public StateMachine<PlayerActionState> ChildFsm { get; private set; }

    public PlayerMovementState MovementState { get; private set; }
    public PlayerDodgeState DodgeState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerFallState FallState { get; private set; }
    public PlayerDeadState DeadState { get; private set; }
    public PlayerNormalAttackState NormalAttackState { get; private set; }
    public PlayerHeavyAttackState HeavyAttackState { get; private set; }
    public PlayerHitState HitState { get; private set; }

    #region 构建
    /// <summary>构造两层状态机、8 个状态实例、父子关系绑定与全部 AnyTransition 注册。</summary>
    public void Build()
    {
        #region 父状态机初始化
        Fsm = new StateMachine<PlayerStanceState>();
        var free = new PlayerFreeState(Fsm, _player);
        var combat = new PlayerCombatState(Fsm, _player);
        Fsm.Initialize(free);
        #endregion

        #region 子状态机初始化
        // 子状态机先建引用，再注入初始态实例
        ChildFsm = Fsm.SetChildFsm<PlayerActionState>();
        MovementState = new PlayerMovementState(ChildFsm, _player, _characterController, _verticalMotion);
        DodgeState = new PlayerDodgeState(ChildFsm, _player, _characterController);
        JumpState = new PlayerJumpState(ChildFsm, _player, _characterController, _verticalMotion);
        FallState = new PlayerFallState(ChildFsm, _player, _characterController, _verticalMotion);
        DeadState = new PlayerDeadState(ChildFsm, _player, _characterController);
        NormalAttackState = new PlayerNormalAttackState(ChildFsm, _player, _characterController);
        HeavyAttackState = new PlayerHeavyAttackState(ChildFsm, _player, _characterController);
        HitState = new PlayerHitState(ChildFsm, _player, _characterController);
        ChildFsm.Initialize(MovementState);
        #endregion

        #region 父层状态机状态转换
        // 父层：进入/退出战斗姿态
        Fsm.AddTransition(free, combat, () => _stateControl.CombatLocked, ETransitionPriority.Normal);
        Fsm.AddTransition(combat, free, () => !_stateControl.CombatLocked, ETransitionPriority.Normal);
        #endregion

        #region 子层状态机状态转换
        // 子层：死亡为 High 优先级任意态抢断，注册先于 Dodge/Jump 使其先命中、可打断一切动作(含 mid-dodge 死亡)
        ChildFsm.AddAnyTransition(DeadState,
            () => _player.StatsControl.IsDead && !_stateControl.IsBusy, ETransitionPriority.High);
        // 受击为 High 优先级任意态抢断；进行中不可再次进入(互斥)；死亡态下禁止触发
        ChildFsm.AddAnyTransition(HitState,
            () => _stateControl.HitRequested && !_stateControl.IsBusy && !_player.StatsControl.IsDead, ETransitionPriority.High);
        // 闪避/跳跃为 High 优先级任意态抢断；进行中不可再次进入(互斥)；死亡态下禁止触发
        // 注册顺序 Dodge 先于 Jump，使 Dodge 优先级略高
        ChildFsm.AddAnyTransition(DodgeState,
            () => _stateControl.DodgeRequested && !_stateControl.IsBusy && !_player.StatsControl.IsDead, ETransitionPriority.High);
        ChildFsm.AddAnyTransition(JumpState,
            () => _stateControl.JumpRequested && !_stateControl.IsBusy && _verticalMotion.CanJump && !_player.StatsControl.IsDead,
            ETransitionPriority.High);
        // 轻击为 High 优先级任意态抢断；进行中不可再次进入(互斥)；死亡态/无右手装备禁用；任意姿态(Free/Combat)均可触发
        ChildFsm.AddAnyTransition(NormalAttackState,
            () => _stateControl.NormalAttackRequested && !_stateControl.IsBusy && !_player.StatsControl.IsDead &&
                  _player.EquipmentControl.GetEquippedItem(EEquipmentSlot.RightHand) != null,
            ETransitionPriority.High);
        // 重击(蓄力)为 High 优先级任意态抢断；进入后由状态内部计时；死亡态/无右手装备/耐力不足禁用；任意姿态均可触发
        ChildFsm.AddAnyTransition(HeavyAttackState,
            () => _stateControl.HeavyAttackRequested && !_stateControl.IsBusy && !_player.StatsControl.IsDead &&
                   _player.EquipmentControl.GetEquippedItem(EEquipmentSlot.RightHand) != null &&
                  _player.StatsControl.CanAffordStamina(_player.StatsControl.HeavyAttackStaminaCost),
            ETransitionPriority.High);
        #endregion
    }
    #endregion
}

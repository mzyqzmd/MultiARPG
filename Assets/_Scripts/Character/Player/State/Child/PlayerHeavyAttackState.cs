using UnityEngine;

/// <summary>
/// 玩家重击（蓄力）状态：进入置 HeavyAttackState bool 触发控制器进入 Heavy_Charge（起手蓄力），
/// 控制器内部按动画过渡链 Charge→Hold→Release→回 Locomotion 自动推进三段（C# 不跟踪阶段）。
/// 蓄力结算倍率仍由 PlayerStateControl.EndHeavyChargeByTier（动画事件 OnHeavyChargeTier）写入右手武器，并关闭取消窗口。
/// 松开重击键：取消窗口(_canCancel)内取消（不挥，直接退出），结算后(Hold/Release 阶段)释放收尾由控制器播完自然回移动态。
/// 动画经 AnimatorController 状态机驱动，由 ClientNetworkAnimator（owner 权威）原生跨端同步。
/// </summary>
public class PlayerHeavyAttackState : PlayerActionState
{

    public PlayerHeavyAttackState(StateMachine<PlayerActionState> stateMachine_,
        Player player_, CharacterController characterController_) :
        base(stateMachine_, player_, characterController_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        _player.StateControl.ConsumeHeavyAttack();
        _player.StateControl.IsBusy = true;
        _player.Anim.SetBool("HeavyAttackState", true);
    }

    public override void Exit()
    {
        base.Exit();
        _player.Anim.SetBool("HeavyAttackState", false);
        _player.Anim.SetBool("IsChargingHeavyAttack", false);
        _player.StateControl.ConsumeHeavyAttack();
        _player.StateControl.IsBusy = false;
    }
}

using UnityEngine;

/// <summary>玩家死亡态：停摆移动并驱动死亡动画参数 DeadState；复活后自动切回移动态。</summary>
public class PlayerDeadState : PlayerActionState
{
    public PlayerDeadState(StateMachine<PlayerActionState> stateMachine_,
        Player player_, CharacterController characterController_) :
        base(stateMachine_, player_, characterController_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        _player.InputService.DisableAllMode();
        _player.UIService.SpawnUiView<UI_PopText>("PopText", EUILayerCanvas.Overlay).
            Initialize("寄!", Color.red, 1.5f, false);
        _player.Anim.SetBool("DeadState", true);
    }

    public override void Exit()
    {
        base.Exit();
        _player.InputService.EnablePlayerMode();
        _player.Anim.SetBool("DeadState", false);
    }
}

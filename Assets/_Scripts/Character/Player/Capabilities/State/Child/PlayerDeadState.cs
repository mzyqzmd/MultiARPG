using UnityEngine;

/// <summary>玩家死亡态：停摆移动并驱动死亡动画参数 DeadState；复活后自动切回移动态。</summary>
public class PlayerDeadState : PlayerActionState
{
    public PlayerDeadState(StateMachine<PlayerActionState> stateMachine_,
        PlayerStateControl stateControl_, CharacterController characterController_,
        PlayerStatsControl statsControl_, Animator anim_, InputService inputService_,
        PlayerCameraService playerCameraService_, AudioService audioService_,
        UIService uiService_, VFXService vfxService_) : base(stateMachine_, stateControl_,
            characterController_, statsControl_, anim_, inputService_, playerCameraService_,
            audioService_, uiService_, vfxService_)
    {
    }

    public override void Enter()
    {
        _inputService.DisableAllMode();
        _uiService.SpawnUiView<UI_PopText>("PopText", EUILayerCanvas.Overlay).
            Initialize("寄!", Color.red, 1.5f, false);
        _anim.SetBool("DeadState", true);
    }

    public override void Exit()
    {
        _inputService.EnablePlayerMode();
        _anim.SetBool("DeadState", false);
    }
}

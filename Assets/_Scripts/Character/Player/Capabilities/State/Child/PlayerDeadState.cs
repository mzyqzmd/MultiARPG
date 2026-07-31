using UnityEngine;

/// <summary>玩家死亡态：停摆移动并驱动死亡动画参数 DeadState；复活后自动切回移动态。</summary>
public class PlayerDeadState : PlayerActionState
{
    private readonly UIService _uiService;
    public PlayerDeadState(StateMachine<PlayerActionState> stateMachine_,
        PlayerStateControl playerState_, CharacterController characterController_,
        PlayerStatsControl playerStats_, Animator anim_, InputService inputService_,
        PlayerCameraService playerCameraService_, AudioService audioService_, UIService uiService_) :
        base(stateMachine_, playerState_, characterController_, playerStats_, anim_,
            inputService_, playerCameraService_, audioService_)
    {
        _uiService = uiService_;
    }

    public override void Enter()
    {
        _inputService.DisableAllMode();
        _playerStateControl.IsDeadState = true;
        _uiService.SpawnUiView<UI_PopText>("PopText", EUILayerCanvas.Overlay).
            Initialize("寄!", Color.red, 1.5f, false);
        _anim.SetBool("DeadState", true);
    }

    public override void Exit()
    {
        _inputService.EnablePlayerMode();
        _playerStateControl.IsDeadState = false;
        _anim.SetBool("DeadState", false);
    }
}

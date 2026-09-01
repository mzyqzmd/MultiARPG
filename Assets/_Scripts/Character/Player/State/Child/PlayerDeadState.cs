using UnityEngine;

/// <summary>玩家死亡态：停摆移动并驱动死亡动画参数 DeadState；复活后自动切回移动态。</summary>
public class PlayerDeadState : PlayerActionState
{
    private float _respawnTimer;

    public PlayerDeadState(StateMachine<PlayerActionState> stateMachine_,
        Player player_, CharacterController characterController_) :
        base(stateMachine_, player_, characterController_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        _respawnTimer = 0f;
        _player.InputService.DisableAllMode();
        _player.AudioService.PlayAudio(_player.PlayerPresentation.DeathAudioGroup,
        _player.PlayerPresentation.DeathAudioKey, _player.transform.position);
        _player.UIService.SpawnUiView<UI_PopText>("PopText", EUILayerCanvas.Overlay).
            Initialize("寄!", Color.red, 1.5f, false);
        _player.Anim.SetBool("DeadState", true);
    }

    public override void Update()
    {
        base.Update();
        if (!_player.IsOwner) return;
        _respawnTimer += Time.deltaTime;
        if (_respawnTimer >= _player.StatsControl.RespawnDuration)
            _player.Respawn();
    }

    public override void Exit()
    {
        base.Exit();
        _player.InputService.EnablePlayerMode();
        _player.Anim.SetBool("DeadState", false);
    }
}

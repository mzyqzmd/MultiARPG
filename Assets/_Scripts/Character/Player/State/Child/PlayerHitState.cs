using UnityEngine;

public class PlayerHitState : PlayerActionState
{
    // 受击动画 Speed Multiplier 参数名：须在 AC_Player.controller 的 Hit 状态上绑定（m_SpeedParameter:HitAnimSpeed），
    // 使受击动画恰好在 _statsControl.HitDuration（可被硬直延长效果修正）内播完。
    private const string HitAnimSpeedParam = "HitAnimSpeed";

    private bool _animSpeedReady;
    private float _hitClipLength;

    public PlayerHitState(StateMachine<PlayerActionState> stateMachine_,
        Player player_, CharacterController characterController_) :
        base(stateMachine_, player_, characterController_)
    {
    }

    public override void Enter()
    {
        base.Enter();
        _player.StateControl.IsBusy = true;
        _player.StateControl.ConsumeHit();
        // 受击硬直时长由配置（SO_CharacterStats.HitDuration）驱动，可被硬直延长类效果经 EStatType.HitDuration 修正
        _stateDuration = _player.StatsControl.HitDuration;
        _player.Anim.SetBool("HitState", true);
        // 按受击方向选动画：0=前 1=后 2=左 3=右（多方向状态机由编辑器后续搭建，此处仅预留参数）
        _player.Anim.SetFloat("HitDirection", _player.StateControl.PendingHitDirection);
        // 已缓存 clip 长度则立即设定倍速（用当前 HitDuration 实时算，确保硬直延长效果生效）
        if (_hitClipLength > 0f)
            _player.Anim.SetFloat(HitAnimSpeedParam, _hitClipLength / _player.StatsControl.HitDuration);
    }

    public override void Exit()
    {
        base.Exit();
        _player.Anim.SetBool("HitState", false);
        _player.StateControl.IsBusy = false;
    }

    public override void Update()
    {
        base.Update();
        if (!_player.IsOwner) return;
        // 首次 IsTag("Hit") 命中后取 blend tree 当前真实 clip 长度缓存一次（blend tree 下 stateInfo.length 是归一化时长，
        // 必须用 clipInfo 取真实秒长；四方向 clip 长度一致，取第一个即可）；倍速用当前 HitDuration 实时算
        if (!_animSpeedReady)
        {
            if (_player.Anim.GetCurrentAnimatorStateInfo(0).IsTag("Hit"))
            {
                var clips = _player.Anim.GetCurrentAnimatorClipInfo(0);
                if (clips.Length > 0)
                {
                    _hitClipLength = clips[0].clip.length;
                    _player.Anim.SetFloat(HitAnimSpeedParam, _hitClipLength / _player.StatsControl.HitDuration);
                    _animSpeedReady = true;
                }
            }
        }
        // 受击为短时长一次性动作，时长到回移动态
        if (_stateDuration <= 0) _stateMachine.ChangeState(_player.StateControl.MovementState);
    }
}

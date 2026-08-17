using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class FakeManHitState : IState
{
    private readonly StateMachine<IState> _fsm;
    private readonly FakeManStatsControl _fakeManStatsControl;
    private readonly FakeMan _fakeMan;
    private readonly Animator _anim;
    private float _stateDuration;
    public FakeManHitState(StateMachine<IState> fsm_, FakeMan fakeMan_,
        FakeManStatsControl fakeManStatsControl_, Animator anim_)
    {
        _fsm = fsm_;
        _fakeMan = fakeMan_;
        _fakeManStatsControl = fakeManStatsControl_;
        _anim = anim_;
    }

    public void Enter()
    {
        _stateDuration = _fakeManStatsControl.HitDuration;
        _anim.SetFloat("HitDirectionX", _fakeMan.PendingHitX);
        _anim.SetFloat("HitDirectionY", _fakeMan.PendingHitY);
        _anim.SetBool("HitState", true);
    }

    public void Exit()
    {
        _anim.SetBool("HitState", false);
    }

    public void Update()
    {
        if (_stateDuration > 0)
            _stateDuration -= Time.deltaTime;
        else
            _fsm.ChangeState(_fakeMan.IdleState);
    }
}

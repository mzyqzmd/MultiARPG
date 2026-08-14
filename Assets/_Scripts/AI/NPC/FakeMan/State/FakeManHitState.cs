using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class FakeManHitState : IState
{
    private readonly StateMachine<IState> _fsm;
    private readonly SO_FakeManStats _soFakeManStats;
    private readonly FakeMan _fakeMan;
    private readonly Animator _anim;
    private float _stateDuration;
    public FakeManHitState(StateMachine<IState> fsm_, FakeMan fakeMan_,
        SO_FakeManStats soFakeManStats_, Animator anim_)
    {
        _fsm = fsm_;
        _fakeMan = fakeMan_;
        _soFakeManStats = soFakeManStats_;
        _anim = anim_;
    }

    public void Enter()
    {
        _stateDuration = _soFakeManStats.HitDuration;
        _anim.SetFloat("HitDirection", _fakeMan.PendingHitDirection);
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

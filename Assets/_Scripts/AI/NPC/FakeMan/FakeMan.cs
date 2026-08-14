using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeMan : Character
{
    [SerializeField] private CapsuleCollider _capsuleCollider;
    [SerializeField] private SO_FakeManStats _soFakeManStats;

    public float PendingHitDirection { get; private set; }

    private FakeManStatsControl _fakeManStatsControl;
    private StateMachine<IState> _fsm;
    public FakeManIdleState IdleState { get; private set; }
    public FakeManHitState HitState { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        _fakeManStatsControl = GetComponent<FakeManStatsControl>();
        _statsControl = _fakeManStatsControl;
        _fsm = new StateMachine<IState>();
        IdleState = new FakeManIdleState(_anim);
        HitState = new FakeManHitState(_fsm, this, _soFakeManStats, _anim);
        _fsm.Initialize(IdleState);
    }

    protected override void Update()
    {
        base.Update();
        _fsm.Update();
    }

    public override void OnHitReceived(DamageInfo damage_, Vector3 contactPoint_)
    {
        base.OnHitReceived(damage_, contactPoint_);
        EnterHitState(contactPoint_);
    }
    public void EnterHitState(Vector3 contactPoint_)
    {
        var local = _capsuleCollider.transform.InverseTransformDirection(
            (contactPoint_ - _capsuleCollider.transform.position).normalized);
        PendingHitDirection = local.z >= Mathf.Abs(local.x) ? (local.z >= 0 ? 0 : 1)
            : (local.x >= 0 ? 3 : 2);
        _fsm.ChangeState(HitState);
        _anim.SetTrigger("Hit");
    }
}

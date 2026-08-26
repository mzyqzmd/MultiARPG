using Unity.Netcode;
using UnityEngine;

public class FakeMan : Character
{
    public SO_FakeManPresentation FakeManPresentation => _presentation as SO_FakeManPresentation;
    private FakeManStatsControl FakeManStatsControl => _statsControl as FakeManStatsControl;

    public FakeManIdleState IdleState { get; private set; }
    public FakeManHitState HitState { get; private set; }
    public float PendingHitX { get; private set; }
    public float PendingHitY { get; private set; }

    private StateMachine<IState> _fsm;

    protected override void Awake()
    {
        base.Awake();
        _fsm = new StateMachine<IState>();
        IdleState = new FakeManIdleState(_anim);
        HitState = new FakeManHitState(_fsm, this, FakeManStatsControl, _anim);
        _fsm.Initialize(IdleState);
    }

    protected override void Update()
    {
        base.Update();
        _fsm.Update();
    }

    public override void OnHitReceived(string effectId_, float multiplier_, Vector3 contactPoint_, bool isStun_ = false)
    {
        base.OnHitReceived(effectId_, multiplier_, contactPoint_, isStun_);
        if (_statsControl.IsDead) _statsControl.ApplyDefaultHealth();
        EnterHitState(contactPoint_);
    }
    public void EnterHitState(Vector3 contactPoint_)
    {
        Vector3 local = transform.InverseTransformDirection(
            contactPoint_ - transform.position);
        Vector2 localVector2 = new Vector2(local.x, local.z).normalized;
        PendingHitX = localVector2.x;
        PendingHitY = localVector2.y;
        _fsm.ChangeState(HitState);
        _anim.SetTrigger("Hit");
    }
}

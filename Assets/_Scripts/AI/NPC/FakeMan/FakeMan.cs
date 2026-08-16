using UnityEngine;

public class FakeMan : Character
{
    [SerializeField] private CapsuleCollider _capsuleCollider;

    private FakeManStatsControl FakeManStatsControl => _statsControl as FakeManStatsControl;

    public FakeManIdleState IdleState { get; private set; }
    public FakeManHitState HitState { get; private set; }
    public float PendingHitDirection { get; private set; }

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

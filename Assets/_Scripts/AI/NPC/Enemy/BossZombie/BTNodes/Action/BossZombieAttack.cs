using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("AI/BossZombie")]
[Name("Boss：攻击（按玩家相对位置选动作）")]
public class BossZombieAttack : ActionTask<BossZombie>
{
    private float _elapsed;
    private bool _hitOpened;
    private EBossZombieAttackAction _selected;

    protected override void OnExecute()
    {
        _elapsed = 0f;
        _hitOpened = false;
        _selected = SelectActionByRelativePosition();
        EnableHitByAction(_selected);
        agent.AnimationControl.RequestAction(_selected);
    }

    protected override void OnUpdate()
    {
        _elapsed += Time.deltaTime;
        float windup = agent.BossZombieStatsControl.AttackWindup;
        float window = agent.BossZombieStatsControl.AttackWindow;
        float duration = agent.BossZombieStatsControl.AttackDuration;

        if (!_hitOpened && _elapsed >= windup)
        {
            _hitOpened = true;
            EnableHitByAction(_selected);
        }
        if (_hitOpened && _elapsed >= windup + window)
            agent.DisableAllHit();

        if (_elapsed >= duration)
            EndAction(true);
    }

    protected override void OnStop(bool resumed_)
    {
        agent.AnimationControl.ForceEndAttack();
    }

    /// <summary>按玩家相对 Boss 的方位 + 当前阶段选离散攻击动作。</summary>
    private EBossZombieAttackAction SelectActionByRelativePosition()
    {
        if (agent.TargetPlayer == null) return EBossZombieAttackAction.Attack01;
        Vector3 local = agent.transform.InverseTransformDirection(
            agent.TargetPlayer.position - agent.transform.position).normalized;
        bool inFront = local.z > 0.3f;
        bool inBack = local.z < -0.3f;
        bool onLeft = local.x < -0.3f;
        bool onRight = local.x > 0.3f;
        float dist = Vector3.Distance(agent.transform.position, agent.TargetPlayer.position);

        if (agent.Phase == EBossZombiePhase.Phase1)
        {
            if (inBack) return EBossZombieAttackAction.Attack03;
            return Random.Range(0, 2) == 0 ? EBossZombieAttackAction.Attack01 : EBossZombieAttackAction.Attack02;
        }
        // Phase2：手脚技
        if (dist > agent.BossZombieStatsControl.GetAttackRange(EBossZombieAttackAction.DashAtk) * 0.8f)
            return EBossZombieAttackAction.DashAtk;
        if (onLeft) return EBossZombieAttackAction.FootLeft;
        if (onRight) return EBossZombieAttackAction.FootRight;
        if (inFront) return EBossZombieAttackAction.DoubleHand;
        return EBossZombieAttackAction.Ground;
    }

    /// <summary>按动作开对应碰撞体组（P1 武器 / P2 手脚）。</summary>
    private void EnableHitByAction(EBossZombieAttackAction action_)
    {
        switch (action_)
        {
            case EBossZombieAttackAction.Attack01:
            case EBossZombieAttackAction.Attack02:
            case EBossZombieAttackAction.Attack03:
                agent.EnableWeaponHit();
                break;
            case EBossZombieAttackAction.DashAtk:
            case EBossZombieAttackAction.DoubleHand:
            case EBossZombieAttackAction.FootLeft:
            case EBossZombieAttackAction.FootRight:
            case EBossZombieAttackAction.Ground:
                agent.EnableHandHit();
                agent.EnableFootHit();
                break;
        }
    }
}

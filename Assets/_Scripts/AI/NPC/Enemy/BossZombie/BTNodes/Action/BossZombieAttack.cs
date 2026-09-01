using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

/// <summary>
/// Boss 僵尸攻击节点：按玩家相对位置选 Attack01/02/03 并请求播放，等待攻击动画时长后结束。
/// 攻击期间锁定朝向（由攻击动画自身驱动转向与位移，不再持续面向玩家）。行为树/NavMesh 仅 Host 端驱动。
/// </summary>
[Category("AI/BossZombie")]
[Name("Boss：攻击")]
public class BossZombieAttack : ActionTask<BossZombie>
{
    /// <summary>玩家处于 Boss 背后多大夹角（度）内时发动向后攻击 Attack03。</summary>
    public float _backAttackAngle = 100f;

    private float _timer;
    private EBossZombieAttackAction _selected;

    protected override void OnExecute()
    {
        _timer = 0f;
        agent.NavAgent.updateRotation = false;
        agent.NavAgent.ResetPath();
        agent.FaceTarget();
        _selected = SelectActionByRelativePosition();
        agent.AnimationControl.RequestAction(_selected);
    }

    protected override void OnUpdate()
    {
        if (!agent.IsServer) return;
        if (agent.TargetPlayer == null)
        {
            agent.AnimationControl.ForceEndAttack();
            EndAction(false);
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= agent.BossZombieStatsControl.AttackDuration)
        {
            agent.AnimationControl.ForceEndAttack();
            EndAction(true);
        }
    }

    protected override void OnStop(bool resumed_)
    {
        agent.AnimationControl.ForceEndAttack();
        agent.NavAgent.updateRotation = agent.Phase == EBossZombiePhase.Phase2;
        agent.NavAgent.ResetPath();
    }

    /// <summary>按玩家相对 Boss 的世界夹角选 P1 攻击动作：背后一定角度内→Attack03（向后攻击），否则 Attack01/02 随机。</summary>
    private EBossZombieAttackAction SelectActionByRelativePosition()
    {
        Vector3 toPlayer = (agent.TargetPlayer.position - agent.transform.position).normalized;
        toPlayer.y = 0f;
        // Boss 正前方（forward）与"指向玩家方向"的夹角：≥ 阈值一半 表示玩家在背后
        float angle = Vector3.Angle(agent.transform.forward, toPlayer);
        if (angle >= _backAttackAngle * 0.5f) return EBossZombieAttackAction.Attack03;
        return Random.Range(0, 2) == 0 ? EBossZombieAttackAction.Attack01 : EBossZombieAttackAction.Attack02;
    }
}

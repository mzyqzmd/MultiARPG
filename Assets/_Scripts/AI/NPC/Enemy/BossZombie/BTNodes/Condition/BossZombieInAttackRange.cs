using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("AI/BossZombie")]
[Name("Boss：是否在攻击范围内")]
public class BossZombieInAttackRange : ConditionTask<BossZombie>
{
    [Tooltip("攻击动作,只有dash和Ground是长距离")]
    public EBossZombieAttackAction _action;
    protected override bool OnCheck()
    {
        if (agent.TargetPlayer == null) return false;
        float attackRange = agent.BossZombieStatsControl.GetAttackRange(_action);
        return Vector3.Distance(agent.transform.position, agent.TargetPlayer.position) <= attackRange;
    }
}

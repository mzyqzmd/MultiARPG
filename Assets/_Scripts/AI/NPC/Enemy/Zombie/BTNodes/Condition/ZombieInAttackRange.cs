using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("AI/Zombie")]
[Name("僵尸：是否在攻击范围内")]
public class ZombieInAttackRange : ConditionTask<Zombie>
{
    protected override bool OnCheck()
    {
        if (agent.TargetPlayer == null) return false;
        return Vector3.Distance(agent.transform.position,
            agent.TargetPlayer.position) <= agent.ZombieStatsControl.AttackRange;
    }
}

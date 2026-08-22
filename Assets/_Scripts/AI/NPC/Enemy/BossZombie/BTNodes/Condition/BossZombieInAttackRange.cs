using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("AI/BossZombie")]
[Name("Boss：是否在指定攻击距离内")]
public class BossZombieInAttackRange : ConditionTask<BossZombie>
{
    [RequiredField]
    public EBossZombieAttackAction Attack;

    protected override bool OnCheck()
    {
        if (agent.TargetPlayer == null) return false;
        return Vector3.Distance(agent.transform.position, agent.TargetPlayer.position)
            <= agent.BossZombieStatsControl.GetAttackRange(Attack);
    }
}

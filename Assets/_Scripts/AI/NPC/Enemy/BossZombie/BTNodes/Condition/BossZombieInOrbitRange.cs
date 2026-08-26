using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("AI/BossZombie")]
[Name("Boss：是否在环绕距离内")]
public class BossZombieInOrbitRange : ConditionTask<BossZombie>
{
    /// <summary>环绕距离系数：维持 attackRange 的此倍数作为停留中心距离。</summary>
    public float _orbitRangeMultiplier = 2f;

    protected override bool OnCheck()
    {
        if (agent.TargetPlayer == null) return false;
        float attackRange = agent.BossZombieStatsControl.GetAttackRange(EBossZombieAttackAction.Attack01);
        float orbitRange = attackRange * _orbitRangeMultiplier;
        return Vector3.Distance(agent.transform.position, agent.TargetPlayer.position) <= orbitRange;
    }
}

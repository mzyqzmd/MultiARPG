using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("AI/BossZombie")]
[Name("Boss：发现可见玩家")]
public class BossZombieCanSeePlayer : ConditionTask<BossZombie>
{
    protected override bool OnCheck()
    {
        if (!agent.IsServer) return false;
        Transform best = null;
        float bestSqr = float.MaxValue;
        var npcPos = agent.transform.position;

        foreach (var player in Services.Get<AINpcService>().PlayerTransforms)
        {
            if (player == null || !agent.CanSeeTarget(player)) continue;
            float sqr = (player.position - npcPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = player;
            }
        }

        agent.TargetPlayer = best;
        return best != null;
    }
}

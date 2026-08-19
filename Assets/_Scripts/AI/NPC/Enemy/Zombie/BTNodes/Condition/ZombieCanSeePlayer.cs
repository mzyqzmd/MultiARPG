using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("AI/Zombie")]
[Name("僵尸：发现可见玩家")]
public class ZombieCanSeePlayer : ConditionTask<Zombie>
{
    #region 从所有在线玩家中挑选真实可见且距离最近者作为追击目标
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
    #endregion
}

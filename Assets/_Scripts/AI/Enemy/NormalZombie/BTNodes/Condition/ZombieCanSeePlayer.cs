using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Name("僵尸：能否看见玩家")]
public class ZombieCanSeePlayer : ConditionTask<Zombie>
{
    protected override bool OnCheck()
        => agent.TargetPlayer != null && agent.Vision.CanSeePlayer(agent.TargetPlayer);
}

using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/Zombie")]
[Name("僵尸：能否看见玩家")]
public class ZombieCanSeePlayer : ConditionTask<Zombie>
{
    protected override bool OnCheck()
        => agent.CanSeeTarget(agent.TargetPlayer);
}

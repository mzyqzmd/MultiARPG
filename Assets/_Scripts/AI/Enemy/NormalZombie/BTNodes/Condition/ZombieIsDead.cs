using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Name("僵尸：是否死亡")]
public class ZombieIsDead : ConditionTask<Zombie>
{
    protected override bool OnCheck()
        => agent.StatsControl != null && agent.StatsControl.IsDead;
}

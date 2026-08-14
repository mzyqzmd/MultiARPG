using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Name("僵尸：是否受伤")]
public class ZombieIsHurt : ConditionTask<Zombie>
{
    protected override bool OnCheck()
        => agent.StatsControl != null && agent.StatsControl.CurrentHealth < agent.StatsControl.MaxHealth;
}

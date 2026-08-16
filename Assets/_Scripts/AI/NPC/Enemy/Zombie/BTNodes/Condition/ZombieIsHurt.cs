using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/Zombie")]
[Name("僵尸：是否受伤")]
public class ZombieIsHurt : ConditionTask<Zombie>
{
    protected override bool OnCheck()
        => agent.ZombieStatsControl.CurrentHealth < agent.ZombieStatsControl.MaxHealth;
}

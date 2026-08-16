using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/Zombie")]
[Name("僵尸：是否死亡")]
public class ZombieIsDead : ConditionTask<Zombie>
{
    protected override bool OnCheck()
        => agent.ZombieStatsControl.IsDead;
}

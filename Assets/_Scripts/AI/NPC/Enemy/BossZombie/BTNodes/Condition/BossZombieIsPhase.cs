using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/BossZombie")]
[Name("Boss：是否处于指定阶段")]
public class BossZombieIsPhase : ConditionTask<BossZombie>
{
    public EBossZombiePhase Phase;

    protected override bool OnCheck() => agent.Phase == Phase;
}

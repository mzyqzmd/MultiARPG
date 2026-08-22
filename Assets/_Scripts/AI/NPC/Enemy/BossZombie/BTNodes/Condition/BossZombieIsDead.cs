using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/BossZombie")]
[Name("Boss：是否死亡")]
public class BossZombieIsDead : ConditionTask<BossZombie>
{
    protected override bool OnCheck() => agent.Phase == EBossZombiePhase.Dead || agent.BossZombieStatsControl.IsDead;
}

using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/BossZombie")]
[Name("Boss：移向玩家")]
public class BossZombieMoveToPlayer : ActionTask<BossZombie>
{
    protected override void OnExecute()
    {
        if (agent.TargetPlayer == null)
        {
            EndAction(false);
            return;
        }
        agent.NavAgent.SetDestination(agent.TargetPlayer.position);
        EndAction(true);
    }
}

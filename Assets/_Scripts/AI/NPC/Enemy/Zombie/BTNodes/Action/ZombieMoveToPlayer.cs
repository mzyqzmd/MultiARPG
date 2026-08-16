using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/Zombie")]
[Name("僵尸：移向玩家")]
public class ZombieMoveToPlayer : ActionTask<Zombie>
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

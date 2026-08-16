using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/Zombie")]
[Name("僵尸：行走")]
public class ZombieSetWalk : ActionTask<Zombie>
{
    protected override void OnExecute()
    {
        agent.NavAgent.speed = agent.ZombieStatsControl.GetMoveSpeed(false);
        agent.AnimationControl.SetWalk();
        EndAction(true);
    }
}

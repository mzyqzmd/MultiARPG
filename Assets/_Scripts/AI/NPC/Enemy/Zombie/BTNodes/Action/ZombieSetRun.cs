using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/Zombie")]
[Name("僵尸：奔跑")]
public class ZombieSetRun : ActionTask<Zombie>
{
    protected override void OnExecute()
    {
        agent.NavAgent.speed = agent.ZombieStatsControl.GetMoveSpeed(true);
        agent.AnimationControl.SetRunning();
        EndAction(true);
    }
}

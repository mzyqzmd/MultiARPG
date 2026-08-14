using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Name("僵尸：奔跑")]
public class ZombieSetRun : ActionTask<Zombie>
{
    protected override void OnExecute()
    {
        agent.ApplyMoveSpeed(true);
        agent.AnimBridge.SetRunning();
        EndAction(true);
    }
}

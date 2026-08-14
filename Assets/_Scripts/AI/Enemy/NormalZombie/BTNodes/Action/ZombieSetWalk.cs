using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Name("僵尸：行走")]
public class ZombieSetWalk : ActionTask<Zombie>
{
    protected override void OnExecute()
    {
        agent.ApplyMoveSpeed(false);
        agent.AnimBridge.SetMoving();
        EndAction(true);
    }
}

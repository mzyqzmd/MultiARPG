using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Name("僵尸：待机")]
public class ZombieIdle : ActionTask<Zombie>
{
    protected override void OnExecute()
    {
        agent.AnimBridge.SetIdle();
        EndAction(true);
    }
}

using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/Zombie")]
[Name("僵尸：待机")]
public class ZombieSetIdle : ActionTask<Zombie>
{
    protected override void OnExecute()
    {
        agent.AnimationControl.SetIdle();
        EndAction(true);
    }
}

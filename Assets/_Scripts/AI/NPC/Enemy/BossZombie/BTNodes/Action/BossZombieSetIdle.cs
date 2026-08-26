using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/BossZombie")]
[Name("Boss：待机")]
public class BossZombieSetIdle : ActionTask<BossZombie>
{
    protected override void OnExecute()
    {
        agent.AnimationControl.SetIdle();
        EndAction(true);
    }
}

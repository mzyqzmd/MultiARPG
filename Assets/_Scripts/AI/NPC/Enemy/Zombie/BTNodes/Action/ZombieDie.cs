using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Category("AI/Zombie")]
[Name("僵尸：死亡")]
public class ZombieDie : ActionTask<Zombie>
{
    protected override void OnExecute()
    {
        agent.Die();
        EndAction(true);
    }
}

using NodeCanvas.Framework;
using ParadoxNotion.Design;

[Name("僵尸：攻击前摇")]
public class ZombieWaitBeforeAttack : ActionTask<Zombie>
{
    [UnityEngine.SerializeField] private float _windup = 0.4f;
    private float _elapsed;

    protected override void OnExecute()
    {
        _elapsed = 0f;
    }

    protected override void OnUpdate()
    {
        _elapsed += UnityEngine.Time.deltaTime;
        if (_elapsed >= _windup) EndAction(true);
    }
}

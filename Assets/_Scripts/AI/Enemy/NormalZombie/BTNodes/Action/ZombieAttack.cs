using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Name("僵尸：攻击")]
public class ZombieAttack : ActionTask<Zombie>
{
    [SerializeField] private float _attackDuration = 1f;
    private float _elapsed;

    protected override void OnExecute()
    {
        agent.PerformAttack();
        _elapsed = 0f;
    }

    protected override void OnUpdate()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _attackDuration) EndAction(true);
    }
}

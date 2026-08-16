using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("AI/Zombie")]
[Name("僵尸：攻击")]
public class ZombieAttack : ActionTask<Zombie>
{
    protected override void OnExecute()
    {
        int dir = ComputeAttackDirection();
        int variant = Random.Range(0, 3);
        agent.AnimationControl.TriggerAttack(dir, variant);
        agent.OnAttackEnded += FinishAttack;
    }

    protected override void OnStop(bool resumed_)
    {
        agent.OnAttackEnded -= FinishAttack;
    }

    /// <summary>攻击动画结束事件回调：反订阅并收口行为树节点。</summary>
    private void FinishAttack()
    {
        agent.OnAttackEnded -= FinishAttack;
        EndAction(true);
    }

    /// <summary>计算攻击方向：玩家相对僵尸本地坐标系的方位（0前/1后/2右/3左）。</summary>
    private int ComputeAttackDirection()
    {
        if (agent.TargetPlayer == null) return 0;
        Vector3 local = agent.transform.InverseTransformDirection(
            (agent.TargetPlayer.position - agent.transform.position).normalized);
        if (local.z >= Mathf.Abs(local.x) && local.z >= 0) return 0; // 前
        if (local.z < 0) return 1;                                  // 后
        return local.x >= 0 ? 2 : 3;                               // 右/左
    }
}

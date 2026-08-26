using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

/// <summary>
/// Boss 僵尸移动节点（纯移动语义）：玩家在环绕距离（orbitRange）外时直冲靠近；
/// 进入环绕距离内则停步并结束本节点（交由上级 Selector 转停留计时节点）。
/// 不含任何攻击判断。行为树/NavMesh 仅 Host 端驱动。
/// </summary>
[Category("AI/BossZombie")]
[Name("Boss：接近玩家")]
public class BossZombieMove : ActionTask<BossZombie>
{
    /// <summary>环绕距离系数：维持 attackRange 的此倍数作为停留中心距离。</summary>
    public float _orbitRangeMultiplier = 2f;

    protected override void OnExecute() => agent.NavAgent.updateRotation = false;

    protected override void OnUpdate()
    {
        if (!agent.IsServer) return;
        if (agent.TargetPlayer == null)
        {
            agent.NavAgent.ResetPath();
            EndAction(false);
            return;
        }

        FaceTarget();
        float attackRange = agent.BossZombieStatsControl.GetAttackRange(EBossZombieAttackAction.Attack01);
        float orbitRange = attackRange * _orbitRangeMultiplier;
        float dist = Vector3.Distance(agent.transform.position, agent.TargetPlayer.position);

        // 贴脸：立即中断，交由上级 Selector 转攻击分支（最高优先级）
        if (dist <= attackRange)
        {
            agent.NavAgent.ResetPath();
            EndAction(false);
            return;
        }

        // 环绕距离外直冲靠近；进入环绕距离内停步结束，转停留计时节点
        if (dist > orbitRange)
            agent.NavAgent.SetDestination(agent.TargetPlayer.position);
        else
        {
            agent.NavAgent.ResetPath();
            EndAction(true);
        }
    }

    protected override void OnStop(bool resumed_)
    {
        agent.NavAgent.updateRotation = agent.Phase == EBossZombiePhase.Phase2;
    }

    /// <summary>水平面向玩家（忽略 Y 轴），关闭 NavAgent 自动转向后手动转向。</summary>
    private void FaceTarget()
    {
        Vector3 dir = agent.TargetPlayer.position - agent.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        agent.transform.rotation = Quaternion.LookRotation(dir);
    }
}

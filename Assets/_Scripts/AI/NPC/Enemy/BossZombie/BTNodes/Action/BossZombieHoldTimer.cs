using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

/// <summary>
/// Boss 僵尸停留计时节点：进入环绕距离后维持距离做侧向绕圈走位（播放左右动画），
/// 停留 _holdDuration 秒后结束本节点（交由上级 Sequence 转攻击节点）。
/// 计时期间玩家退到环绕距离外则中断转回接近，贴脸则中断转攻击。行为树/NavMesh 仅 Host 端驱动。
/// </summary>
[Category("AI/BossZombie")]
[Name("Boss：停留计时")]
public class BossZombieHoldTimer : ActionTask<BossZombie>
{
    /// <summary>环绕距离系数：维持 attackRange 的此倍数作为停留中心距离。</summary>
    public float _orbitRangeMultiplier = 2f;
    /// <summary>进入环绕距离后停留时长（秒）：停步维持距离后再择机攻击。</summary>
    public float _holdDuration = 2f;
    /// <summary>停留环绕时的绕圈方向：+1 顺时针、-1 逆时针（维持侧向走位以播放左右动画）。</summary>
    public float _strafeSign = 1f;

    private float _timer;

    protected override void OnExecute()
    {
        _timer = 0f;
        agent.NavAgent.updateRotation = agent.Phase != EBossZombiePhase.Phase2;
    }

    protected override void OnUpdate()
    {
        if (!agent.IsServer) return;
        if (agent.TargetPlayer == null)
        {
            agent.NavAgent.ResetPath();
            EndAction(false);
            return;
        }

        if (agent.Phase != EBossZombiePhase.Phase2) FaceTarget();
        float attackRange = agent.BossZombieStatsControl.GetAttackRange(EBossZombieAttackAction.Attack01);
        float orbitRange = attackRange * _orbitRangeMultiplier;
        float dist = Vector3.Distance(agent.transform.position, agent.TargetPlayer.position);

        // 贴脸：立即中断，转攻击分支（最高优先级）
        if (dist <= attackRange)
        {
            agent.NavAgent.ResetPath();
            EndAction(false);
            return;
        }

        // 玩家退到环绕距离外：中断，转回接近节点
        if (dist > orbitRange)
        {
            agent.NavAgent.ResetPath();
            EndAction(false);
            return;
        }

        // 沿"与玩家连线"的切线方向绕圈，维持 orbitRange 距离 → NavAgent.velocity 带侧向分量驱动左右动画
        Vector3 toPlayer = agent.TargetPlayer.position - agent.transform.position;
        toPlayer.y = 0f;
        Vector3 tangent = Vector3.Cross(Vector3.up, toPlayer).normalized * _strafeSign;
        agent.NavAgent.SetDestination(agent.TargetPlayer.position + tangent * orbitRange);

        _timer += Time.deltaTime;
        if (_timer >= _holdDuration)
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

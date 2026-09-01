using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

/// <summary>
/// Boss 僵尸 P2 攻击节点：丢武器狂暴态下，按玩家距离分两档选动作——
/// 近战档（≤近战距离）在 Ground 与随机拳脚间掷骰；远距离档在 DashAtk 突进与 Ground 砸地间掷骰。
/// 仅 Ground 在动画末帧经动画事件 spawn 冲击波（其余 4 动作不触发）；攻击期间关 Nav 更新交 Root Motion。
/// 行为树/NavMesh 仅 Host 端驱动。
/// </summary>
[Category("AI/BossZombie")]
[Name("Boss：P2 攻击")]
public class BossZombiePhase2Attack : ActionTask<BossZombie>
{
    /// <summary>远距离档发动 DashAtk 突进的概率（0~1），其余为 Ground 砸地。调低可减少突进占比。</summary>
    [Tooltip("远距离时 DashAtk 的占比，其余为 Ground。调低可减少突进频率。")]
    public float _dashChance = 0.5f;
    /// <summary>近战档发动 Ground 砸地的概率（0~1），其余为随机拳脚。</summary>
    [Tooltip("近战时 Ground 砸地的占比，其余为随机拳脚。")]
    public float _groundChance = 0.25f;

    private float _timer;
    private EBossZombieAttackAction _selected;

    protected override void OnExecute()
    {
        _timer = 0f;
        agent.NavAgent.updateRotation = false;
        agent.NavAgent.ResetPath();
        agent.FaceTarget();
        _selected = SelectActionByDistance();
        agent.AnimationControl.RequestAction(_selected);
    }

    protected override void OnUpdate()
    {
        if (!agent.IsServer) return;
        if (agent.TargetPlayer == null)
        {
            agent.AnimationControl.ForceEndAttack();
            EndAction(false);
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= agent.BossZombieStatsControl.AttackDuration)
        {
            agent.AnimationControl.ForceEndAttack();
            EndAction(true);
        }
    }

    protected override void OnStop(bool resumed_)
    {
        agent.AnimationControl.ForceEndAttack();
        agent.NavAgent.updateRotation = agent.Phase == EBossZombiePhase.Phase2;
        agent.NavAgent.ResetPath();
    }

    /// <summary>
    /// 按玩家距离分两档选 P2 动作。本节点由行为树在 DashAtk/Ground 距离（长）内触发，
    /// 故以近战距离（短）为分流阈值：超出则够不着拳脚，只在 DashAtk 与 Ground 两个长距动作间掷骰；
    /// 进入近战范围后按概率选 Ground 或随机拳脚。
    /// 注意：DashAtk 不可独占远距离档，否则因长距区间远大于近战区间，实战中几乎恒选 DashAtk。
    /// </summary>
    private EBossZombieAttackAction SelectActionByDistance()
    {
        if (agent.TargetPlayer == null) return EBossZombieAttackAction.DoubleHand;
        float dist = Vector3.Distance(agent.transform.position, agent.TargetPlayer.position);
        float meleeRange = agent.BossZombieStatsControl.GetAttackRange(EBossZombieAttackAction.DoubleHand);

        // 远距离档：拳脚够不着，只在两个长距动作间分配。
        if (dist > meleeRange)
            return Random.value < _dashChance
                ? EBossZombieAttackAction.DashAtk
                : EBossZombieAttackAction.Ground;

        if (Random.value < _groundChance) return EBossZombieAttackAction.Ground;
        return Random.Range(0, 3) switch
        {
            0 => EBossZombieAttackAction.DoubleHand,
            1 => EBossZombieAttackAction.FootLeft,
            _ => EBossZombieAttackAction.FootRight,
        };
    }
}

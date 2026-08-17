using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("AI/Zombie")]
[Name("僵尸：攻击")]
public class ZombieAttack : ActionTask<Zombie>
{
    private float _elapsed;
    private bool _hitOpened;

    protected override void OnExecute()
    {
        _elapsed = 0f;
        _hitOpened = false;
        Vector2 dirXY = ComputeAttackDirectionXY();
        int variant = Random.Range(0, 3);
        agent.AnimationControl.TriggerAttack(dirXY, variant);
    }

    protected override void OnUpdate()
    {
        _elapsed += Time.deltaTime;
        float windup = agent.ZombieStatsControl.AttackWindup;
        float window = agent.ZombieStatsControl.AttackWindow;
        float duration = agent.ZombieStatsControl.AttackDuration;

        // 前摇结束开启判定碰撞体（仅一次）
        if (!_hitOpened && _elapsed >= windup)
        {
            _hitOpened = true;
            agent.EnableHandHit();
        }
        // 判定窗口结束关闭碰撞体
        if (_hitOpened && _elapsed >= windup + window)
            agent.DisableHandHit();

        // 攻击总时长结束收口
        if (_elapsed >= duration)
            EndAction(true);
    }

    protected override void OnStop(bool resumed_)
    {
        // 防御节点被 BT 中断：强制复位攻击态所有副作用
        agent.AnimationControl.ForceEndAttack();
    }

    /// <summary>计算攻击方向：玩家相对僵尸本地坐标系的连续坐标（X=右、Y=前，范围约[-1,1]），供 Cartesian Blend Tree 方位/斜向混合。</summary>
    private Vector2 ComputeAttackDirectionXY()
    {
        if (agent.TargetPlayer == null) return new Vector2(0f, 1f);
        Vector3 local = agent.transform.InverseTransformDirection(
            agent.TargetPlayer.position - agent.transform.position);
        return new Vector2(local.x, local.z).normalized;
    }
}

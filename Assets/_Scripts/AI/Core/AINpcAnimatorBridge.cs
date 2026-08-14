using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 行为树节点唯一的 Animator 参数出口：节点只调语义方法（SetIdle/SetMoving/SetRunning/TriggerAttack/SetDead），
/// 内部统一互斥清理 + SetBool，避免各节点散落设参数导致冲突。
/// 攻击时临时禁用 NavMeshAgent 的位置/旋转更新，交给 Root Motion 接管，结束还原。
/// </summary>
public class AINpcAnimatorBridge : MonoBehaviour
{
    [SerializeField] private Animator _anim;
    [SerializeField] private NavMeshAgent _navAgent;

    private const string IdleState = "IdleState";
    private const string MovingState = "MovingState";
    private const string RunningState = "RunningState";
    private const string AttackTrigger = "Attack";
    private const string DeadState = "DeadState";
    private const string AttackDirection = "AttackDirection";

    private bool _attacking;

    #region 语义方法（行为树节点调用）
    /// <summary>进入待机：停 Agent 速度并标记 Idle 动画。</summary>
    public void SetIdle()
    {
        if (_attacking) return;
        _navAgent.ResetPath();
        _anim.SetBool(MovingState, false);
        _anim.SetBool(RunningState, false);
        _anim.SetBool(IdleState, true);
    }

    /// <summary>进入行走：标记 Moving 动画（NavMeshAgent 仍驱动位移）。</summary>
    public void SetMoving()
    {
        if (_attacking) return;
        _anim.SetBool(IdleState, false);
        _anim.SetBool(RunningState, false);
        _anim.SetBool(MovingState, true);
    }

    /// <summary>进入奔跑：标记 Running 动画（受伤追击时使用）。</summary>
    public void SetRunning()
    {
        if (_attacking) return;
        _anim.SetBool(IdleState, false);
        _anim.SetBool(MovingState, false);
        _anim.SetBool(RunningState, true);
    }

    /// <summary>触发攻击：按方向(0前/1后/2左/3右)置参数 + 禁用 Agent 更新交给 Root Motion，结束由 EndAttack 还原。</summary>
    public void TriggerAttack(int direction_)
    {
        _attacking = true;
        _navAgent.ResetPath();
        _navAgent.updatePosition = false;
        _navAgent.updateRotation = false;
        _anim.SetBool(IdleState, false);
        _anim.SetBool(MovingState, false);
        _anim.SetBool(RunningState, false);
        _anim.SetInteger(AttackDirection, direction_);
        _anim.SetTrigger(AttackTrigger);
    }

    /// <summary>攻击动画结束（由攻击窗口帧事件调用）：恢复 Agent 更新并复位攻击态。</summary>
    public void EndAttack()
    {
        if (!_attacking) return;
        _attacking = false;
        _navAgent.updatePosition = true;
        _navAgent.updateRotation = true;
        // 攻击结束后回到行走准备姿态
        _anim.SetBool(MovingState, true);
    }

    /// <summary>进入死亡：标记 Dead 动画并停 Agent。</summary>
    public void SetDead()
    {
        _navAgent.ResetPath();
        _navAgent.enabled = false;
        _anim.SetBool(IdleState, false);
        _anim.SetBool(MovingState, false);
        _anim.SetBool(RunningState, false);
        _anim.SetBool(DeadState, true);
    }
    #endregion
}

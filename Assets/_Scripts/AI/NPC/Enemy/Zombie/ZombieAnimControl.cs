using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 行为树节点唯一的 Animator 参数出口：节点只调语义方法（SetIdle/SetMoving/SetRunning/TriggerAttack/SetDead），
/// 内部统一互斥清理 + SetBool，避免各节点散落设参数导致冲突。
/// 攻击时临时禁用 NavMeshAgent 的位置/旋转更新，交给 Root Motion 接管，结束还原。
/// </summary>
public class ZombieAnimControl : MonoBehaviour
{
    [SerializeField] private Zombie _zombie;

    private const string MoveSpeed = "MoveSpeed";
    private const string AttackTrigger = "Attack";
    private const string AttackDirection = "AttackDirection";
    private const string ForwardAttackVariant = "ForwardAttackVariant";
    private const string HitTrigger = "Hit";
    private const string HitDirection = "HitDirection";
    private const string DeadState = "DeadState";

    private float _currentSpeed;
    private float _targetSpeed;
    private bool _attacking;


    private void Update()
    {
        if (_attacking) return;
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, Time.deltaTime * 5f);
        _zombie.Anim.SetFloat(MoveSpeed, _currentSpeed);
    }

    #region 语义方法（行为树节点调用）
    /// <summary>进入待机：停 Agent 路径并置 MoveSpeed=0（Locomotion Blend Tree 回到 Idle）。</summary>
    public void SetIdle()
    {
        if (_attacking) return;
        _zombie.NavAgent.ResetPath();
        _targetSpeed = 0;
    }

    /// <summary>进入行走：置 MoveSpeed=1（Locomotion Blend Tree 混合到 Walk）。</summary>
    public void SetWalk()
    {
        if (_attacking) return;
        _targetSpeed = 1;
    }

    /// <summary>进入奔跑：置 MoveSpeed=2（Locomotion Blend Tree 混合到 Run，受伤追击）。</summary>
    public void SetRunning()
    {
        if (_attacking) return;
        _targetSpeed = 2;
    }

    /// <summary>触发攻击：按方向(0前/1后/2右/3左)与变体(前向0~2随机)置参数 + 禁用 Agent 更新交给 Root Motion，结束由 EndAttack 还原。</summary>
    public void TriggerAttack(int direction_, int variant_ = 0)
    {
        _attacking = true;
        _zombie.Anim.applyRootMotion = true;
        _zombie.NavAgent.ResetPath();
        _zombie.NavAgent.updatePosition = false;
        _zombie.NavAgent.updateRotation = false;
        _zombie.Anim.SetFloat(MoveSpeed, 0f);
        _zombie.Anim.SetInteger(AttackDirection, direction_);
        _zombie.Anim.SetInteger(ForwardAttackVariant, variant_);
        _zombie.Anim.SetTrigger(AttackTrigger);
    }

    public void EnableHandHit() => _zombie.EnableHandHit();

    /// <summary>攻击窗口关闭（动画帧事件调用）：禁用手部碰撞体并交还 Agent 位移控制。</summary>
    public void DisableHandHit()
    {
        _zombie.DisableHandHit();
        if (!_attacking) return;
        _attacking = false;
        _zombie.Anim.applyRootMotion = false;
        _zombie.NavAgent.Warp(_zombie.transform.position);
        _zombie.NavAgent.updatePosition = true;
        _zombie.NavAgent.updateRotation = true;
        // 攻击结束后回到行走准备姿态
        _zombie.Anim.SetFloat(MoveSpeed, 1f);
        _zombie.OnAttackEnded?.Invoke();
    }

    /// <summary>受击表现：按方向(0前/1后/2左/3右)置 HitDirection 并触发 Hit Blend Tree（不交 Root Motion，NavMesh 仍驱动）。</summary>
    public void SetHit(int direction_)
    {
        _zombie.Anim.SetInteger(HitDirection, direction_);
        _zombie.Anim.SetTrigger(HitTrigger);
    }

    /// <summary>进入死亡：标记 Dead 动画并停 Agent。</summary>
    public void SetDead()
    {
        _zombie.NavAgent.ResetPath();
        _zombie.NavAgent.enabled = false;
        _zombie.Anim.SetFloat(MoveSpeed, 0f);
        _zombie.Anim.SetBool(DeadState, true);
    }
    #endregion
}

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
    private const string AttackDirectionX = "AttackDirectionX";
    private const string AttackDirectionY = "AttackDirectionY";
    private const string ForwardAttackVariant = "ForwardAttackVariant";
    private const string HitTrigger = "Hit";
    private const string HitDirectionX = "HitDirectionX";
    private const string HitDirectionY = "HitDirectionY";
    private const string DeadState = "DeadState";

    private float _currentSpeed;
    private float _targetSpeed;
    private bool _attacking;


    private void Update()
    {
        // 仅 Host 端由 BT 写 MoveSpeed；客户端 MoveSpeed 由 NetworkAnimator 同步值驱动，本地不写以防双端抢写。
        if (!_zombie.IsServer || _attacking) return;
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

    /// <summary>触发攻击：按方向(X=右/Y=前,范围[-1,1])与变体(前向0~2随机)置参数 + 禁用 Agent 更新交给 Root Motion，结束由 EndAttack 还原。</summary>
    public void TriggerAttack(Vector2 dirXY_, float variant_ = 0f)
    {
        _attacking = true;
        _zombie.Anim.applyRootMotion = true;
        _zombie.NavAgent.ResetPath();
        _zombie.NavAgent.updatePosition = false;
        _zombie.NavAgent.updateRotation = false;
        _zombie.Anim.SetFloat(MoveSpeed, 0f);
        _zombie.Anim.SetFloat(AttackDirectionX, dirXY_.x);
        _zombie.Anim.SetFloat(AttackDirectionY, dirXY_.y);
        _zombie.Anim.SetFloat(ForwardAttackVariant, variant_);
        _zombie.Anim.SetTrigger(AttackTrigger);
    }

    /// <summary>打断安全网：幂等复位攻击态所有副作用（_attacking/碰撞体/RootMotion/Agent 标志/MoveSpeed），被 SetHit/SetDead/ZombieAttack.OnStop 调用。</summary>
    public void ForceEndAttack()
    {
        if (!_attacking) return;
        _attacking = false;
        _zombie.Anim.SetFloat(MoveSpeed, 1f);
        _zombie.DisableHandHit();
        _zombie.Anim.applyRootMotion = false;
        _zombie.NavAgent.Warp(_zombie.transform.position);
        _zombie.NavAgent.updatePosition = true;
        _zombie.NavAgent.updateRotation = true;
    }

    /// <summary>受击表现：按接触点相对本地坐标的连续方向(X=右/Y=前,范围[-1,1])置 HitDirectionX/Y 双 float 并触发 Hit Blend Tree（不交 Root Motion，NavMesh 仍驱动）；入口先复位攻击态防打断卡死。</summary>
    public void SetHit(Vector3 contactPoint_)
    {
        ForceEndAttack();
        Vector3 local = _zombie.transform.InverseTransformDirection(
            contactPoint_ - _zombie.transform.position).normalized;
        Vector2 localVector2 = new Vector2(local.x, local.z).normalized;
        _zombie.Anim.SetFloat(HitDirectionX, localVector2.x);
        _zombie.Anim.SetFloat(HitDirectionY, localVector2.y);
        _zombie.Anim.SetTrigger(HitTrigger);
    }

    /// <summary>进入死亡：标记 Dead 动画并停 Agent；入口先复位攻击态防打断卡死。</summary>
    public void SetDead()
    {
        ForceEndAttack();
        _zombie.NavAgent.ResetPath();
        _zombie.NavAgent.enabled = false;
        _zombie.Anim.SetFloat(MoveSpeed, 0f);
        _zombie.Anim.SetBool(DeadState, true);
    }
    #endregion
}

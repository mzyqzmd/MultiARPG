using UnityEngine;

/// <summary>
/// Boss 僵尸动画桥（语义出口）。BT 节点只调语义方法（SetIdle/RequestAction/SetHit/SetDead 等），
/// 内部统一互斥清理 + 设参数，避免各节点散落。
/// 移动八方向 BlendTree 输入（ForwardInput/RightInput）仅 Host 端由 NavMeshAgent.velocity 投影算出；
/// 攻击期间临时关 Nav 更新交 Root Motion，结束还原。
/// </summary>
public class BossZombieAnimControl : MonoBehaviour
{
    [SerializeField] private BossZombie _bossZombie;

    private const string ActionTrigger = "ActionTrigger";
    private const string ActionIndex = "ActionIndex";
    private const string ForwardInput = "ForwardInput";
    private const string RightInput = "RightInput";
    private const string HitTrigger = "HitTrigger";
    private const string HitDirectionX = "HitDirectionX";
    private const string HitDirectionY = "HitDirectionY";
    private const string DeadState = "DeadState";

    private bool _attacking;

    /// <summary>每帧推进：仅 Host 端由 NavAgent.velocity 投影算八方向 BlendTree 输入（客户端经 NetworkAnimator 同步参数）；攻击中交 Root Motion 驱动位移，跳过写入避免干扰。</summary>
    private void Update()
    {
        if (!_bossZombie.IsServer) return;
        if (_attacking) return;
        var velocity = _bossZombie.NavAgent.velocity;
        if (velocity.sqrMagnitude < 0.01f)
        {
            _bossZombie.Anim.SetFloat(ForwardInput, 0f);
            _bossZombie.Anim.SetFloat(RightInput, 0f);
            return;
        }
        var local = _bossZombie.transform.InverseTransformDirection(velocity);
        float maxSpeed = _bossZombie.BossZombieStatsControl.GetMoveSpeed(false);
        _bossZombie.Anim.SetFloat(ForwardInput, Mathf.Clamp(local.z / maxSpeed, -1f, 1f));
        _bossZombie.Anim.SetFloat(RightInput, Mathf.Clamp(local.x / maxSpeed, -1f, 1f));
    }

    #region 语义方法（行为树节点调用）
    /// <summary>进入待机：停 Agent 路径（Blend Tree 回 idle）。</summary>
    public void SetIdle()
    {
        if (_attacking) return;
        _bossZombie.NavAgent.ResetPath();
    }

    /// <summary>触发离散攻击：按动作枚举写 ActionTrigger(int)，并临时关 Nav 更新交 Root Motion，结束由 ForceEndAttack 还原。</summary>
    public void RequestAction(EBossZombieAttackAction action_)
    {
        _attacking = true;
        _bossZombie.NavAgent.ResetPath();
        _bossZombie.NavAgent.updatePosition = false;
        _bossZombie.NavAgent.updateRotation = false;
        _bossZombie.Anim.applyRootMotion = true;
        _bossZombie.Anim.SetInteger(ActionIndex, (int)action_);
        _bossZombie.Anim.SetTrigger(ActionTrigger);
        _bossZombie.AudioService.PlayAudio(_bossZombie.BossZombiePresentation.AttackAudioGroup,
            _bossZombie.BossZombiePresentation.AttackAudioKey, _bossZombie.transform.position);
    }

    /// <summary>打断安全网：幂等复位攻击态副作用（碰撞体/RootMotion/Agent 标志）。</summary>
    public void ForceEndAttack()
    {
        if (!_attacking) return;
        _attacking = false;
        _bossZombie.NavAgent.updateRotation = true;
        _bossZombie.NavAgent.updatePosition = true;
        _bossZombie.Anim.applyRootMotion = false;
        if (_bossZombie.NavAgent.isOnNavMesh)
            _bossZombie.NavAgent.Warp(_bossZombie.transform.position);
        _bossZombie.DisableWeaponHit();
    }

    /// <summary>受击表现：按接触点方向置 HitDirectionX/Y 双 float 触发 2D BlendTree（前/后/左/右）。</summary>
    public void SetHit(Vector3 contactPoint_)
    {
        Vector3 local = _bossZombie.transform.InverseTransformDirection(
            contactPoint_ - _bossZombie.transform.position).normalized;
        _bossZombie.Anim.SetFloat(HitDirectionX, local.x);
        _bossZombie.Anim.SetFloat(HitDirectionY, local.z);
        _bossZombie.Anim.SetTrigger(HitTrigger);
    }

    /// <summary>进入死亡：停 Agent + 标记 Dead 动画。</summary>
    public void SetDead()
    {
        ForceEndAttack();
        _bossZombie.NavAgent.ResetPath();
        _bossZombie.NavAgent.enabled = false;
        _bossZombie.Anim.SetBool(DeadState, true);
        _bossZombie.AudioService.PlayAudio(_bossZombie.BossZombiePresentation.DeathAudioGroup,
            _bossZombie.BossZombiePresentation.DeathAudioKey, _bossZombie.transform.position);
    }
    #endregion

    #region 动画事件
    /// <summary>动画事件入口（Attack clip 关键帧调用）：开启武器碰撞体，交动画掌控命中窗口。</summary>
    public void EnableWeaponHitEvent() => _bossZombie.EnableWeaponHit();

    /// <summary>动画事件入口（Attack clip 关键帧调用）：关闭武器碰撞体，交动画掌控命中窗口。</summary>
    public void DisableWeaponHitEvent() => _bossZombie.DisableWeaponHit(); 
    #endregion
}

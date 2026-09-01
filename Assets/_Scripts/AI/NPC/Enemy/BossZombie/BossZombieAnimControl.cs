using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Boss 僵尸动画桥（语义出口）。BT 节点只调语义方法（SetIdle/RequestAction/SetHit/SetDead 等），
/// 内部统一互斥清理 + 设参数，避免各节点散落。
/// 移动八方向 BlendTree 输入（ForwardInput/RightInput）仅 Host 端由 NavMeshAgent.velocity 投影算出；
/// 攻击期间临时关 Nav 更新交 Root Motion，期间改由 NavMesh 钳制位移（水平防穿墙、垂直防升天），结束还原。
/// </summary>
public class BossZombieAnimControl : MonoBehaviour
{
    [SerializeField] private BossZombie _bossZombie;

    private const string ActionTrigger = "ActionTrigger";
    private const string ActionIndex = "ActionIndex";
    private const string ForwardInput = "ForwardInput";
    private const string RightInput = "RightInput";
    private const string HitTrigger = "Hit";
    private const string HitDirectionX = "HitDirectionX";
    private const string HitDirectionY = "HitDirectionY";
    private const string DeadState = "DeadState";
    private const string DownTrigger = "DownTrigger";
    /// <summary>地面高度采样半径（米）：覆盖攻击动画的 Y 轴 Root Motion 幅度；过大易误吸到远处地面，过小则陡坡处采样失败。</summary>
    private const float GroundSampleDistance = 2f;

    private bool _attacking;
    private Vector3 _lastNavPosition;

    /// <summary>每帧推进：仅 Host 端由 NavAgent.velocity 投影算八方向 BlendTree 输入（客户端经 NetworkAnimator 同步参数）；攻击中改做 Root Motion 钳制，跳过写入避免干扰。</summary>
    private void Update()
    {
        if (!_bossZombie.IsServer) return;
        if (_attacking)
        {
            ClampRootMotionToNavMesh();
            return;
        }
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

    /// <summary>
    /// Root Motion 钳制：攻击期间 applyRootMotion 由动画直接驱动 transform，不受 NavMesh/NavAgent 约束。
    /// 水平：对「上一合法位置→当前位置」做 NavMesh 线段检测，被边界阻断则钳到边界点，防突进动作（DashAtk）冲出可行走区域/穿墙。
    /// 垂直：钳到 NavMesh 地面高度，防动画自带 Y 轴 Root Motion（突进/砸地含起跳轨迹）让 Boss 升天或陷入地面。
    /// 位置经 NetworkTransform（server 权威）自然同步到客户端，故仅 Host 端钳制即可。
    /// </summary>
    private void ClampRootMotionToNavMesh()
    {
        Vector3 to = _bossZombie.transform.position;

        if (NavMesh.Raycast(_lastNavPosition, to, out NavMeshHit blockHit, NavMesh.AllAreas))
            to = blockHit.position;

        // 采样失败时沿用上一合法高度，避免 Y 轴逐帧累积漂移。
        to.y = NavMesh.SamplePosition(to, out NavMeshHit groundHit, GroundSampleDistance, NavMesh.AllAreas)
            ? groundHit.position.y
            : _lastNavPosition.y;

        _bossZombie.transform.position = to;
        _lastNavPosition = to;
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
        _lastNavPosition = _bossZombie.transform.position;
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
        // 按当前阶段关对应碰撞体（P1 武器/右手，P2 拳脚）。
        if (_bossZombie.Phase == EBossZombiePhase.Phase2)
            _bossZombie.DisableHandAndFootHit();
        else
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

    /// <summary>
    /// 阶段切换演出：触发下蹲→起身（AC 内 Down→Rise→Movement 链路），期间停 Agent 路径。
    /// 先清空残留的 Hit trigger：它若留在队列里，会在 Down 过渡起步后的下一帧把状态抢回 Hit，导致倒下动画播不出来。
    /// </summary>
    public void SetTransitioning()
    {
        if (_attacking) ForceEndAttack();
        _bossZombie.NavAgent.ResetPath();
        _bossZombie.Anim.ResetTrigger(HitTrigger);
        _bossZombie.Anim.SetTrigger(DownTrigger);
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
}

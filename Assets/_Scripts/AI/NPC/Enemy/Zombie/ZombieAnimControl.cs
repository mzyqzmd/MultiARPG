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
    /// <summary>受击动画 Speed Multiplier 参数名：须在 AC_Zombie.controller 的 Hit 状态上绑定（m_SpeedParameter:HitAnimSpeed），使受击动画恰好在 HitDuration（可被效果修正）内播完。</summary>
    private const string HitAnimSpeedParam = "HitAnimSpeed";
    /// <summary>免疫判定阈值：HitDuration 小于等于此值视为「受击打断免疫」（被 ZombieHitStun 效果压到 0），跳过打断与受击动画仍扣血。</summary>
    private const float HitStunImmuneThreshold = 0.001f;
    private const string DeadState = "DeadState";

    private AudioService _audioService;
    private float _currentSpeed;
    private float _targetSpeed;
    private bool _attacking;
    private bool _hitAnimSpeedReady;
    private float _hitClipLength;

    private void Awake()
    {
        _audioService = Services.Get<AudioService>();
    }
    /// <summary>每帧推进：免疫期无需处理；非免疫期首次 Hit 动画命中后取真实 clip 长度并设倍速（blend tree 下 stateInfo.length 为归一化值，须用 clipInfo 取真实秒长）。</summary>
    private void Update()
    {
        if (!_zombie.IsServer) return;
        if (_zombie.ZombieStatsControl.HitDuration > HitStunImmuneThreshold &&
            !_hitAnimSpeedReady && _zombie.Anim.GetCurrentAnimatorStateInfo(0).IsName("Hit"))
        {
            var clips = _zombie.Anim.GetCurrentAnimatorClipInfo(0);
            if (clips.Length > 0)
            {
                _hitClipLength = clips[0].clip.length;
                _zombie.Anim.SetFloat(HitAnimSpeedParam, _hitClipLength / _zombie.ZombieStatsControl.HitDuration);
                _hitAnimSpeedReady = true;
            }
        }
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, 0.5f);
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
        // Warp 需 agent 已在有效 NavMesh 上，否则在失活/未烘焙场景抛错；仅复位位置标记位即可。
        if (_zombie.NavAgent.isOnNavMesh)
            _zombie.NavAgent.Warp(_zombie.transform.position);
        _zombie.NavAgent.updatePosition = true;
        _zombie.NavAgent.updateRotation = true;
    }

    /// <summary>受击表现：免疫期（HitDuration 被效果压到 0）跳过打断与受击动画，仅由 base 扣血；正常期按接触点方向置 HitDirectionX/Y 双 float 触发 Hit Blend Tree，并按「clip 真实长度 / HitDuration」动态设受击动画倍速，使硬直时长与配置一致。</summary>
    public void SetHit(Vector3 contactPoint_)
    {
        // 免疫期：不打断攻击、不播受击动画（base.OnHitReceived 仍扣血）。
        if (_zombie.ZombieStatsControl.HitDuration <= HitStunImmuneThreshold) return;
        Vector3 local = _zombie.transform.InverseTransformDirection(
            contactPoint_ - _zombie.transform.position).normalized;
        Vector2 localVector2 = new Vector2(local.x, local.z).normalized;
        _zombie.Anim.SetFloat(HitDirectionX, localVector2.x);
        _zombie.Anim.SetFloat(HitDirectionY, localVector2.y);
        _zombie.Anim.SetTrigger(HitTrigger);
        // 已缓存 clip 长度则立即按当前 HitDuration 设倍速（确保硬直缩短/延长效果实时生效）
        if (_hitClipLength > 0f)
            _zombie.Anim.SetFloat(HitAnimSpeedParam, _hitClipLength / _zombie.ZombieStatsControl.HitDuration);
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

    #region 音效（动画事件调用，借 NetworkAnimator 同步跨端）
    /// <summary>idle 循环音效：从僵尸表现配置取 idle key 本地播（动画事件驱动，各端播到该帧各自触发）。</summary>
    public void PlayIdleAudio() =>
        _audioService.PlayAudio(_zombie.ZombiePresentation.IdleAudioGroup,
           _zombie.ZombiePresentation.IdleAudioKey, _zombie.transform.position);
    public void StopIdleAudio()=>
        _audioService.StopAudio(_zombie.ZombiePresentation.IdleAudioGroup,
            _zombie.ZombiePresentation.IdleAudioKey);
    /// <summary>死亡音效：从僵尸表现配置取 death key 本地播。</summary>
    public void PlayDeathAudio() =>
        _audioService.PlayAudio(_zombie.ZombiePresentation.DeathAudioGroup,
            _zombie.ZombiePresentation.DeathAudioKey, _zombie.transform.position);
    /// <summary>攻击音效：从僵尸表现配置取 attack key 本地播。</summary>
    public void PlayAttackAudio() =>
        _audioService.PlayAudio(_zombie.ZombiePresentation.AttackAudioGroup,
            _zombie.ZombiePresentation.AttackAudioKey, _zombie.transform.position);
    #endregion
}

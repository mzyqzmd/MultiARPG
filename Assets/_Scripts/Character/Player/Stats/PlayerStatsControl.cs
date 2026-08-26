using System;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 玩家数据持有者：继承 CharacterStatsControl，注册玩家专属可修正属性（跑速/闪避速度/击晕时长/耐力上限/耐力回复速率），
/// 对外暴露修正后当前值；其余属性保持配置透传（不可运行时修改）。
/// 派生计算（跑/走速度、跳跃初速）与耐力运行时逻辑统一收敛于此，使本类成为玩家属性的"计算中心"。
/// </summary>
public class PlayerStatsControl : CharacterStatsControl
{
    [Header("玩家引用")]
    [SerializeField] private Player _player;

    private SO_PlayerStats PlayerStats => _characterStats as SO_PlayerStats;

    #region 玩家可修改的属性
    private ModifiableStat_Float _runSpeed;
    private ModifiableStat_Float _dodgeSpeed;
    private ModifiableStat_Float _stunDuration;
    private ModifiableStat_Int _maxStamina;
    private ModifiableStat_Int _staminaRegenRate;

    public float RunSpeed => _runSpeed.Value;
    public float DodgeSpeed => _dodgeSpeed.Value;
    public float StunDuration => _stunDuration.Value;
    public int MaxStamina => _maxStamina.Value;
    public int StaminaRegenRate => _staminaRegenRate.Value;
    #endregion

    /// <summary>当前耐力值（纯读取，无副作用；上限收缩夹取见 AddModifier/RemoveModifier override）。</summary>
    public int CurrentStamina { get; private set; }
    private float _staminaAccumulator;  // 连续消耗/回复的小数累加器（达 ±1 整段冲刷）
    private float _idleTimer;           // 累计空闲时间
    /// <summary>归一化比例 0~1，供 UI 进度条。</summary>
    public float StaminaRatio => MaxStamina > 0f ? (float)CurrentStamina / MaxStamina : 0f;
    /// <summary>力竭瞬间事件（耐力刚降到 0），供协调层订阅播放反馈。</summary>
    public event Action OnExhausted;
    public event Action<int, int> OnStaminaChanged;

    #region 配置透传属性（不可运行时修改）
    public float DodgeDuration => PlayerStats.DodgeDuration;
    public int DodgeStaminaCost => PlayerStats.DodgeStaminaCost;
    public float JumpHeight => PlayerStats.JumpHeight;
    public float Gravity => PlayerStats.Gravity;
    public int JumpStaminaCost => PlayerStats.JumpStaminaCost;
    public int NormalAttackStaminaCost => PlayerStats.NormalAttackStaminaCost;
    public float NormalAttackDuration => PlayerStats.NormalAttackDuration;
    public int HeavyAttackStaminaCost => PlayerStats.HeavyAttackStaminaCost;
    public int RunStaminaDrain => PlayerStats.RunStaminaDrain;
    public float StaminaRegenDelay => PlayerStats.StaminaRegenDelay;
    #endregion

    protected override void Awake()
    {
        base.Awake();
        _runSpeed = new(PlayerStats.RunSpeed);
        _dodgeSpeed = new(PlayerStats.DodgeSpeed);
        _stunDuration = new(PlayerStats.StunDuration);
        _maxStamina = new(PlayerStats.MaxStamina);
        _staminaRegenRate = new(PlayerStats.StaminaRegenRate);
        // 修正后的属性注册
        _floatStats.Add(EStatType.RunSpeed, _runSpeed);
        _floatStats.Add(EStatType.DodgeSpeed, _dodgeSpeed);
        _floatStats.Add(EStatType.StunDuration, _stunDuration);
        _intStats.Add(EStatType.MaxStamina, _maxStamina);
        _intStats.Add(EStatType.StaminaRegenRate, _staminaRegenRate);
        ResetStamina();
    }

    #region 派生计算（属性运算集中于此）
    /// <summary>当前水平移动速度：跑步取 RunSpeed，否则取 NormalMoveSpeed。</summary>
    public override float GetMoveSpeed(bool running_) => running_ ? RunSpeed : NormalMoveSpeed;

    /// <summary>跳跃初速度（向上），由跳跃高度与重力加速度推导。</summary>
    public float GetJumpInitialVelocity() => Mathf.Sqrt(JumpHeight * -2f * Gravity);
    #endregion

    #region 耐力（纯本地，不跨客户端同步）

    /// <summary>是否足以支付 amount_ 的耐力消耗。</summary>
    public bool CanAffordStamina(int amount_) => CurrentStamina >= amount_;

    /// <summary>支付 amount_ 耐力（不足则扣到 0），并重置空闲计时（消耗态不回复）。</summary>
    public void SpendStamina(int amount_)
    {
        bool wasExerting = CurrentStamina > 0;
        int current = CurrentStamina;
        CurrentStamina = Mathf.Max(0, CurrentStamina - amount_);
        OnStaminaChanged?.Invoke(current, CurrentStamina);
        _idleTimer = 0f;
        if (wasExerting && CurrentStamina <= 0)
        {
            OnExhausted?.Invoke();
            _player.AudioService.PlayAudio(_player.PlayerPresentation.ExhaustedAudioGroup,
               _player.PlayerPresentation.ExhaustedAudioKey, transform.position);
        }
    }

    /// <summary>持续消耗（跑步）：按速率×帧时累加小数，达整段才冲刷，避免 int 下每帧截断为 0；消耗态不回复。</summary>
    public void DrainStamina(float ratePerSec_, float delta_)
    {
        _idleTimer = 0f;
        _staminaAccumulator -= ratePerSec_ * delta_;
        FlushAccumulator();
    }

    /// <summary>不消耗时调用：累计空闲时间，超过回复延迟后按速率自动回复（速率/上限均取修正后当前值）。</summary>
    public void RecoverStamina(float delta_)
    {
        _idleTimer += delta_;
        if (_idleTimer < StaminaRegenDelay) return;
        _staminaAccumulator += StaminaRegenRate * delta_;
        FlushAccumulator();
    }

    /// <summary>小数累加器冲刷：达 ±1 整段才落地到整数耐力，夹 [0,MaxStamina]；满/空时清零残留防债。</summary>
    private void FlushAccumulator()
    {
        if (Mathf.Abs(_staminaAccumulator) < 1f) return;
        int whole = Mathf.FloorToInt(_staminaAccumulator);
        int current = CurrentStamina;
        CurrentStamina = Mathf.Clamp(CurrentStamina + whole, 0, MaxStamina);
        _staminaAccumulator -= whole;
        if (CurrentStamina == MaxStamina || CurrentStamina == 0) _staminaAccumulator = 0f;
        if (current != CurrentStamina) OnStaminaChanged?.Invoke(current, CurrentStamina);
    }

    /// <summary>重置为满耐力并清空空闲计时与累加器（本地玩家绑定时调用）。</summary>
    public void ResetStamina()
    {
        int current = CurrentStamina;
        CurrentStamina = MaxStamina;
        _staminaAccumulator = 0f;
        OnStaminaChanged?.Invoke(current, CurrentStamina);
        _idleTimer = 0f;
    }
    #endregion

    #region 属性修正（扩基类：耐力上限被减益收缩时立即夹取当前耐力）
    public override void ApplyStatModifier(EStatType type_, StatModifier modifier_)
    {
        base.ApplyStatModifier(type_, modifier_);
        if (type_ == EStatType.MaxStamina) ClampCurrentStaminaOnMaxChanged();
    }

    public override void RemoveStatModifier(EStatType type_, StatModifier modifier_)
    {
        base.RemoveStatModifier(type_, modifier_);
        if (type_ == EStatType.MaxStamina) ClampCurrentStaminaOnMaxChanged();
    }
    /// <summary>生命上限变化后夹取当前生命并触发变更事件（上限收缩时比例随之变化）。</summary>
    private void ClampCurrentStaminaOnMaxChanged()
    {
        int old = CurrentStamina;
        CurrentStamina = Mathf.Clamp(CurrentStamina, 0, MaxStamina);
        if (old != CurrentHealth)
            OnStaminaChanged?.Invoke(old, CurrentHealth);
    }
    #endregion
}

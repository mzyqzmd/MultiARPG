using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色数据持有者（普通类，非 MonoBehaviour）：包装 SO_CharacterStats，对外暴露"当前值"而非配置原始值。
/// 白名单属性（见 EStatType）注册为 ModifiableStat 支持运行时加减乘除修正；其余属性（如 RotationSpeed）保持配置透传。
/// 派生计算（移动速度、转向角速度等）统一收敛于此，避免散落在状态机/运动控制类。
/// Player 与未来 Enemy 各自派生 StatsControl 经 RegisterStat 扩展专属可修正属性与派生逻辑。
/// 同时实现 IEffectSource（攻击者来源）与 IEffectTarget（效果作用目标）：效果最终修改的数据均在此，
/// 故数据持有者即唯一的效果目标/来源。本类为纯本地数据中心，零网络依赖；
/// 数值修改权归 owner 本地，死亡态由本地 _currentHealth 驱动（Character 订阅 OnCurrentHealthChanged 本地判定）。
/// </summary>
public abstract class CharacterStatsControl : MonoBehaviour, IEffectSource, IEffectTarget
{
    [Header("基础配置")]
    [SerializeField] protected SO_CharacterStats _characterStats;
    protected readonly Dictionary<EStatType, ModifiableStat_Int> _intStats = new();
    protected readonly Dictionary<EStatType, ModifiableStat_Float> _floatStats = new();
    protected readonly EffectControl _effectControl = new();
    public EffectControl EffectControl => _effectControl;
    public int CurrentHealth { get; protected set; }
    public bool IsDead => CurrentHealth <= 0;
    public float HealthRatio => MaxHealth > 0f ? (float)CurrentHealth / MaxHealth : 0f;
    public event Action<int, int> OnCurrentHealthChanged;
 
    public float DamageMultiplier { get; set; } = 1f;
    #region 数据配置
    protected float _rotationSpeed;
    protected ModifiableStat_Float _normalMoveSpeed;
    protected ModifiableStat_Int _maxHealth;
    protected ModifiableStat_Float _hitDuration;
    protected ModifiableStat_Int _physicalArmor;
    protected ModifiableStat_Int _magicalArmor;
    public float RotationSpeed => _rotationSpeed;
    public float NormalMoveSpeed => _normalMoveSpeed.Value;
    public int MaxHealth => _maxHealth.Value;
    public float HitDuration => _hitDuration.Value;
    public int PhysicalArmor => _physicalArmor.Value;
    public int MagicalArmor => _magicalArmor.Value;
    #endregion


    protected virtual void Awake()
    {
        _rotationSpeed = _characterStats.RotationSpeed;
        _normalMoveSpeed = new(_characterStats.NormalMoveSpeed);
        _maxHealth = new(_characterStats.MaxHealth);
        _hitDuration = new(_characterStats.HitDuration);
        _physicalArmor = new(_characterStats.PhysicalArmor);
        _magicalArmor = new(_characterStats.MagicalArmor);
        // 白名单属性注册为 ModifiableStat 支持运行时加减乘除修正
        _floatStats.Add(EStatType.NormalMoveSpeed, _normalMoveSpeed);
        _intStats.Add(EStatType.MaxHealth, _maxHealth);
        _floatStats.Add(EStatType.HitDuration, _hitDuration);
        _intStats.Add(EStatType.PhysicalArmor, _physicalArmor);
        _intStats.Add(EStatType.MagicalArmor, _magicalArmor);
        CurrentHealth = MaxHealth;
    }

    /// <summary>组件销毁时归还全部生效效果（含内嵌 timer 的 TickService 注册），避免悬空引用。</summary>
    private void OnDestroy() => _effectControl.ClearAll();

    /// <summary>当前水平移动速度：基类默认步行速度，子类可 override 区分跑/走。</summary>
    public virtual float GetMoveSpeed(bool running_) => NormalMoveSpeed;

    #region 生命值体系（本地权威：数值仅本机修改）
    /// <summary>受伤（IEffectTarget）：先按护甲减法减伤（护甲高于伤害仍至少 1 点），两类各自结算后求和扣血。</summary>
    public void TakeDamage(int physical_, int magic_)
    {
        int physicalDamage = Mathf.Max(0, physical_ - PhysicalArmor);
        int magicDamage = Mathf.Max(0, magic_ - MagicalArmor);
        int totalDamage = Mathf.Max(1, physicalDamage + magicDamage);
        SetCurrentHealth(CurrentHealth - totalDamage);
    }

    /// <summary>治疗（IEffectTarget）：当前生命增长并夹取不超上限。</summary>
    public void Heal(int amount_) =>
        SetCurrentHealth(CurrentHealth + amount_);

    /// <summary>新游戏默认满血初始化。</summary>
    public void ApplyDefaultHealth() =>
        SetCurrentHealth(MaxHealth);

    /// <summary>读档还原当前生命值（夹取到合法范围）。</summary>
    public void RestoreHealth(int value_) =>
        SetCurrentHealth(value_);

    private void SetCurrentHealth(int value_)
    {
        int clamped = Mathf.Clamp(value_, 0, MaxHealth);
        if (clamped == CurrentHealth) return;
        int old = CurrentHealth;
        CurrentHealth = clamped;
        Debug.Log($"Health:{old} -> {CurrentHealth}");
        OnCurrentHealthChanged?.Invoke(old, CurrentHealth);
    }
    #endregion

    #region 属性修正对外接口(StatModifier)
    /// <summary>施加属性修正（IEffectTarget）：本地应用（原始 API，不抛网络事件）。</summary>
    public virtual void ApplyStatModifier(EStatType type_, StatModifier modifier_)
    {
        if (_intStats.TryGetValue(type_, out var intStat))
            intStat.Add(modifier_);
        if (_floatStats.TryGetValue(type_, out var floatStat))
            floatStat.Add(modifier_);
        if (type_ == EStatType.MaxHealth) ClampCurrentHealthOnMaxChanged();
    }

    /// <summary>移除属性修正（IEffectTarget）：本地回滚（原始 API，不抛网络事件）。</summary>
    public virtual void RemoveStatModifier(EStatType type_, StatModifier modifier_)
    {
        if (_intStats.TryGetValue(type_, out var intStat))
            intStat.Remove(modifier_);
        if (_floatStats.TryGetValue(type_, out var floatStat))
            floatStat.Remove(modifier_);
        if (type_ == EStatType.MaxHealth) ClampCurrentHealthOnMaxChanged();
    }

    /// <summary>生命上限变化后夹取当前生命并触发变更事件（上限收缩时比例随之变化）。</summary>
    private void ClampCurrentHealthOnMaxChanged()
    {
        int old = CurrentHealth;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        if (old != CurrentHealth)
            OnCurrentHealthChanged?.Invoke(old, CurrentHealth);
    }
    #endregion
}

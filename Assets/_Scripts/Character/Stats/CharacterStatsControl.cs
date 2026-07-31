using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色数据持有者（普通类，非 MonoBehaviour）：包装 SO_CharacterStats，对外暴露"当前值"而非配置原始值。
/// 白名单属性（见 EStatType）注册为 ModifiableStat 支持运行时加减乘除修正；其余属性（如 RotationSpeed）保持配置透传。
/// 派生计算（移动速度、转向角速度等）统一收敛于此，避免散落在状态机/运动控制类。
/// Player 与未来 Enemy 各自派生 StatsControl 经 RegisterStat 扩展专属可修正属性与派生逻辑。
/// </summary>
public abstract class CharacterStatsControl : IEffectSource
{
    protected readonly Dictionary<EStatType, ModifiableStat> _modifiableStats = new();

    private readonly SO_CharacterStats _stats;
    private int _currentHealth;

    public CharacterStatsControl(SO_CharacterStats stats_)
    {
        _stats = stats_;
        _modifiableStats[EStatType.NormalMoveSpeed] = new(_stats.NormalMoveSpeed);
        _modifiableStats[EStatType.MaxHealth] = new(_stats.MaxHealth);
        _currentHealth = MaxHealth;
    }

    #region 生命值体系（纯本地，不跨端同步）
    /// <summary>当前生命值变化事件（old, new），供 UI 订阅；上限变化夹取后亦会触发。</summary>
    public event Action<int, int> OnCurrentHealthChanged;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => Mathf.RoundToInt(_modifiableStats[EStatType.MaxHealth].Current);
    public float HealthRatio => MaxHealth > 0f ? (float)CurrentHealth / MaxHealth : 0f;
    /// <summary>是否已死亡：本地生命值归零即判定死亡，作为 FSM 统一本地死亡判断来源（不依赖网络往返）。</summary>
    public bool IsDead => _currentHealth <= 0;

    /// <summary>受伤：当前生命减损并夹取不低于 0，触发变更事件。</summary>
    public void TakeDamage(int amount_) =>
        ApplyHealthDelta(-amount_);

    /// <summary>治疗：当前生命增长并夹取不超上限，触发变更事件。</summary>
    public void Heal(int amount_) =>
        ApplyHealthDelta(amount_);

    /// <summary>新游戏默认满血初始化。</summary>
    public void ApplyDefaultHealth() =>
        SetCurrentHealth(MaxHealth);

    /// <summary>读档还原当前生命值（夹取到合法范围）。</summary>
    public void RestoreHealth(int value_) =>
        SetCurrentHealth(value_);

    private void ApplyHealthDelta(int delta_)
    {
        int old = _currentHealth;
        SetCurrentHealth(old + delta_);
    }

    private void SetCurrentHealth(int value_)
    {
        int clamped = Mathf.Clamp(value_, 0, MaxHealth);
        if (clamped == _currentHealth) return;
        int old = _currentHealth;
        _currentHealth = clamped;
        Debug.Log($"{nameof(CharacterStatsControl)}: {old} -> {_currentHealth}");
        OnCurrentHealthChanged?.Invoke(old, _currentHealth);
    }
    #endregion

    public float NormalMoveSpeed => _modifiableStats[EStatType.NormalMoveSpeed].Current;
    public float RotationSpeed => _stats.RotationSpeed;

    #region 派生计算（属性运算集中于此）
    /// <summary>当前水平移动速度：基类默认步行速度，子类可 override 区分跑/走。</summary>
    public virtual float GetMoveSpeed(bool running_) => NormalMoveSpeed;
    /// <summary>单帧转向角速度（度/帧），供动作态 RotateTowards 统一使用。</summary>
    public float GetRotationDelta() => RotationSpeed * Time.deltaTime;
    #endregion

    #region 运行时属性修正 API（由 Character 经效果系统调用）
    /// <summary>施加一条修正（未注册的属性静默忽略，防御 Enemy 收到玩家专属属性修正）。</summary>
    public virtual void AddModifier(EStatType type_, StatModifier modifier_)
    {
        if (!_modifiableStats.TryGetValue(type_, out var stat)) return;
        stat.Add(modifier_);
        if (type_ == EStatType.MaxHealth) ClampCurrentHealthOnMaxChanged();
    }

    /// <summary>移除一条修正（按值匹配，与施加时来自同一效果资产）。</summary>
    public virtual void RemoveModifier(EStatType type_, StatModifier modifier_)
    {
        if (!_modifiableStats.TryGetValue(type_, out var stat)) return;
        stat.Remove(modifier_);
        if (type_ == EStatType.MaxHealth) ClampCurrentHealthOnMaxChanged();
    }

    /// <summary>生命上限变化后夹取当前生命并触发变更事件（上限收缩时比例随之变化）。</summary>
    private void ClampCurrentHealthOnMaxChanged()
    {
        int old = _currentHealth;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, MaxHealth);
        if (old != _currentHealth)
            OnCurrentHealthChanged?.Invoke(old, _currentHealth);
    }
    #endregion

    #region 攻击者来源（IEffectSource 默认 0，子类 override 读具体配置）
    public virtual int PhysicalAttack => 0;
    public virtual int MagicAttack => 0;
    public virtual int FireAttack => 0;
    public virtual int IceAttack => 0;
    public virtual int LightningAttack => 0;
    #endregion
}

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
public abstract class CharacterStatsControl : IEffectSource, IEffectTarget
{
    protected readonly Dictionary<EStatType, ModifiableStat> _modifiableStats = new();

    private readonly SO_CharacterStats _stats;
    private readonly EffectControl _effectControl = new();
    private int _currentHealth;
    public float NormalMoveSpeed => _modifiableStats[EStatType.NormalMoveSpeed].Current;
    public float RotationSpeed => _stats.RotationSpeed;
    /// <summary>受击硬直时长（秒）：配置基础值可被硬直延长 / 缩短类效果经 EStatType.HitDuration 运行时修正。</summary>
    public float HitDuration => _modifiableStats[EStatType.HitDuration].Current;
    public CharacterStatsControl(SO_CharacterStats stats_)
    {
        _stats = stats_;
        _modifiableStats[EStatType.NormalMoveSpeed] = new(_stats.NormalMoveSpeed);
        _modifiableStats[EStatType.MaxHealth] = new(_stats.MaxHealth);
        _modifiableStats[EStatType.HitDuration] = new(_stats.HitDuration);
        _currentHealth = MaxHealth;
    }

    #region 派生计算（属性运算集中于此）
    /// <summary>当前水平移动速度：基类默认步行速度，子类可 override 区分跑/走。</summary>
    public virtual float GetMoveSpeed(bool running_) => NormalMoveSpeed;
    /// <summary>单帧转向角速度（度/帧），供动作态 RotateTowards 统一使用。</summary>
    public float GetRotationDelta() => RotationSpeed * Time.deltaTime;
    #endregion

    #region 生命值体系（本地权威：数值仅本机修改）
    /// <summary>当前生命值变化事件（old, new），供 UI 订阅；上限变化夹取后亦会触发。</summary>
    public event Action<int, int> OnCurrentHealthChanged;
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => Mathf.RoundToInt(_modifiableStats[EStatType.MaxHealth].Current);
    public float HealthRatio => MaxHealth > 0f ? (float)CurrentHealth / MaxHealth : 0f;
    /// <summary>是否已死亡：本地生命值归零即判定死亡，作为 FSM 统一本地死亡判断来源（不依赖网络往返）。</summary>
    public bool IsDead => _currentHealth <= 0;

    /// <summary>受伤（IEffectTarget）：取总伤害减血。</summary>
    public void TakeDamage(DamageInfo damage_) =>
       SetCurrentHealth(_currentHealth - damage_.TotalDamage);

    /// <summary>治疗（IEffectTarget）：当前生命增长并夹取不超上限。</summary>
    public void Heal(int amount_) =>
        SetCurrentHealth(_currentHealth + amount_);

    /// <summary>新游戏默认满血初始化。</summary>
    public void ApplyDefaultHealth() =>
        SetCurrentHealth(MaxHealth);

    /// <summary>读档还原当前生命值（夹取到合法范围）。</summary>
    public void RestoreHealth(int value_) =>
        SetCurrentHealth(value_);

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

    #region EffectControl施加/移除效果对外接口

    /// <summary>施加一个效果：转交 EffectControl 作用于自身（target_ = this）。</summary>
    public void ApplyEffect(SO_BaseEffect effect_, IEffectSource source_) =>
        _effectControl.ApplyEffect(effect_, this, source_);

    /// <summary>按效果 id 手动移除（与 ApplyEffect 对称）：撤除对应 ActiveEffect 并触发其 OnRemove 回滚（如 Static 属性修正减回）。</summary>
    public void RemoveEffect(string id_) =>
        _effectControl.RemoveEffect(id_, this);
    #endregion

    #region 属性修正对外接口(StatModifier)
    /// <summary>施加属性修正（IEffectTarget）：本地应用（原始 API，不抛网络事件）。</summary>
    public void ApplyStatModifier(EStatType type_, StatModifier modifier_) =>
        AddModifier(type_, modifier_);

    /// <summary>移除属性修正（IEffectTarget）：本地回滚（原始 API，不抛网络事件）。</summary>
    public void RemoveStatModifier(EStatType type_, StatModifier modifier_) =>
        RemoveModifier(type_, modifier_);

    /// <summary>施加一条修正（未注册的属性静默忽略，防御 Enemy 收到玩家专属属性修正）。</summary>
    protected virtual void AddModifier(EStatType type_, StatModifier modifier_)
    {
        if (!_modifiableStats.TryGetValue(type_, out var stat)) return;
        stat.Add(modifier_);
        if (type_ == EStatType.MaxHealth) ClampCurrentHealthOnMaxChanged();
    }

    /// <summary>移除一条修正（按值匹配，与施加时来自同一效果资产）。</summary>
    protected virtual void RemoveModifier(EStatType type_, StatModifier modifier_)
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
}

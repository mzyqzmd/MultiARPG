using Sirenix.OdinInspector;
using System;
using UnityEngine;

/// <summary>
/// 效果数据基类（普通 [Serializable] 类，非 ScriptableObject）。所有具体效果（伤害 / 回复 / 减速 / 灼烧…）继承此类，
/// 在 SO_EffectDatabase 资产内以 OdinSerialize Dictionary&lt;string, BaseEffect&gt; 集中配置（Odin 支持多态 [Serializable] 类序列化）。
/// 运行时由 EffectService.GetEffect(id) 查表并深拷贝返回独立实例，经 EffectControl.ApplyEffect 统一施加（Instant 快速路径 / Timed 池化）。
/// </summary>
[Serializable]
public abstract class BaseEffect
{
    [SerializeField] private EEffectType _effectType;
    [Tooltip("仅 Timed 类型使用：效果持续秒数。非 Timed 时灰显禁用，配置了也不生效。")]
    [EnableIf("@EffectType == EEffectType.Timed")]
    [SerializeField] private float _duration;
    [Tooltip("仅 Timed 类型使用：持续期间 Execute 的触发间隔（秒）。<=0 时退化为一次性（仅施加时 Execute 一次）。")]
    [EnableIf("@EffectType == EEffectType.Timed")]
    [SerializeField] private float _tickInterval;

    public EEffectType EffectType => _effectType;
    public float Duration => EffectType == EEffectType.Timed ? _duration : (EffectType == EEffectType.Static ? float.MaxValue : 0f);
    public float TickInterval => _tickInterval;

    /// <summary>将效果作用于目标。Instant 立即执行一次；Timed 仅由 Apply 执行一次、到期走 OnRemove 回滚；Static 仅由 Apply 执行一次（挂载态，不每帧重作用于）。source_ 为攻击者来源，环境伤害为 null。</summary>
    public abstract void Execute(IEffectTarget target_, IEffectSource source_);

    /// <summary>效果被移除时回滚（如属性修正类减回上限）。默认空实现；仅属性修正类需要 override。source_ 同 Execute。</summary>
    public virtual void OnRemove(IEffectTarget target_, IEffectSource source_) { }

    /// <summary>返回当前 effect 的独立实例（默认浅拷贝，子类字段均为值类型/string，浅拷贝语义上等价于深拷贝）。
    /// EffectService.GetEffect 调此方法从 db 原型克隆出运行时实例，防止多实例并发污染（如 DamageMultiplierEffect._originalMultiplier 写入）。</summary>
    public virtual BaseEffect Clone() => (BaseEffect)MemberwiseClone();
}

using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 效果数据基类（ScriptableObject）。所有具体效果（伤害 / 回复 / 减速 / 灼烧…）继承此类，
/// 在编辑器创建资产并配置参数，注册进 SO_EffectDatabase 字典（key 即效果 id）按 id 查表。
/// 施加时由 EffectControl 调用 Execute，作用于 IEffectTarget（如 Character）。
/// </summary>
public abstract class SO_BaseEffect : ScriptableObject
{
    [SerializeField] private EEffectType _effectType;
    [Tooltip("仅 Timed 类型使用：效果持续秒数。非 Timed 时灰显禁用，配置了也不生效。")]
    [EnableIf("@EffectType == EEffectType.Timed")]
    [SerializeField] private float _duration;

    public EEffectType EffectType => _effectType;
    public float Duration => _duration;

    /// <summary>将效果作用于目标。Instant 立即执行一次；Timed 仅由 Apply 执行一次、到期走 OnRemove 回滚；Static 仅由 Apply 执行一次（挂载态，不每帧重作用于）。source_ 为攻击者来源，环境伤害为 null。</summary>
    public abstract void Execute(IEffectTarget target_, IEffectSource source_);

    /// <summary>效果被移除时回滚（如属性修正型减回上限）。默认空实现；仅属性修正类需要 override。source_ 同 Execute。</summary>
    public virtual void OnRemove(IEffectTarget target_, IEffectSource source_) { }
}

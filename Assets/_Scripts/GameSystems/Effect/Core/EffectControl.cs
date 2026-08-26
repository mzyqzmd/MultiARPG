using System.Collections.Generic;

/// <summary>效果控制器（普通 C# 类）：持有生效中的效果列表与两个对象池（ActiveEffect 池 + EventTimer 池），负责从池借取施加、到期归还。持续效果的周期触发与到期由 ActiveEffect 内嵌的 EventTimer 本地自驱，无需每帧手动驱动。</summary>
public class EffectControl
{
    private readonly List<ActiveEffect> _actives = new();
    private readonly PlainObjectPool<EventTimer> _timerPool;
    private readonly PlainObjectPool<ActiveEffect> _activePool;

    public EffectControl()
    {
        _timerPool = new PlainObjectPool<EventTimer>(() => new EventTimer());
        _activePool = new PlainObjectPool<ActiveEffect>(() => new ActiveEffect(_timerPool));
    }

    #region 对外 API
    /// <summary>所有类型均在施加时执行一次；Timed 由内嵌 EventTimer 周期触发 Execute 并在到期时 OnRemove。source_ 为攻击者来源（环境伤害为 null）。
    /// Instant 效果走快速路径（直接 Execute+OnRemove 不经池化），Timed/Static 走原有 ActiveEffect + EventTimer 池化路径。</summary>
    public void ApplyEffect(BaseEffect effect_, IEffectTarget target_, IEffectSource source_ = null)
    {
        if (effect_.EffectType == EEffectType.Instant)
        {
            effect_.Execute(target_, source_);
            effect_.OnRemove(target_, source_);
            return;
        }
        var activeEffect = _activePool.Get();
        activeEffect.Init(effect_, target_, source_);
        activeEffect.OnEffectComplete += RemoveEffect;
        _actives.Add(activeEffect);
    }

    public void RemoveEffect(ActiveEffect activeEffect_)
    {
        if (!_actives.Remove(activeEffect_))
            return;
        activeEffect_.OnEffectComplete -= RemoveEffect;
        activeEffect_.Release();
        _activePool.Release(activeEffect_);
    }

    /// <summary>角色销毁/离场时调用：归还全部未完成效果（含内嵌 timer），避免悬空 TickService 注册与引用泄漏。</summary>
    public void ClearAll()
    {
        for (int i = _actives.Count - 1; i >= 0; i--)
        {
            var activeEffect = _actives[i];
            activeEffect.OnEffectComplete -= RemoveEffect;
            activeEffect.Release();
            _activePool.Release(activeEffect);
        }
        _actives.Clear();
    }
    #endregion
}

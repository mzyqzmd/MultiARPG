using System.Collections.Generic;

/// <summary>效果控制器（普通 C# 类）：持有生效中的效果列表，负责施加与移除。持续效果的周期触发与到期由 ActiveEffect 内嵌的 EventTimer 本地自驱，无需每帧手动驱动。</summary>
public class EffectControl
{
    private readonly List<ActiveEffect> _actives = new();

    #region 施加一个效果
    /// <summary>所有类型均在施加时执行一次（属性修正/即时伤害由此生效）；Timed 由内嵌 EventTimer 周期触发 Execute 并在到期时 OnRemove。source_ 为攻击者来源（环境伤害为 null）。</summary>
    public void ApplyEffect(SO_BaseEffect effect_, IEffectTarget target_, IEffectSource source_ = null)
    {
        var activeEffect = new ActiveEffect(effect_, target_, source_, this);
        _actives.Add(activeEffect);
    }
    #endregion

    #region 移除效果
    /// <summary>ActiveEffect 内嵌 EventTimer 到期时自驱调用：停止计时器（从 TickService 注销防泄漏）并从列表移除。</summary>
    public void RemoveActive(ActiveEffect activeEffect_)
    {
        activeEffect_.Stop();
        _actives.Remove(activeEffect_);
    }

    /// <summary>按效果 id 手动移除（倒序遍历，命中即停止并移除全部同名匹配，适配叠加层）。Static 效果由此主动撤除；Instant/Timed 同样可按 id 移除。</summary>
    public void RemoveEffect(string id_, IEffectTarget target_)
    {
        for (int i = _actives.Count - 1; i >= 0; i--)
        {
            if (_actives[i].Id == id_)
            {
                _actives[i].Stop();
                _actives.RemoveAt(i);
            }
        }
    }
    #endregion
}

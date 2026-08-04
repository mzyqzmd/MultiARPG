using System.Collections.Generic;

/// <summary>效果控制器（普通 C# 类）：持有生效中的效果列表，负责施加与每帧驱动持续效果。</summary>
public class EffectControl
{
    private readonly List<ActiveEffect> _actives = new();

    #region 每帧驱动持续效果（仅 server 端调用）
    //Timed 计时、到期移除（先 OnRemove 回滚）；Static 为挂载态保留至外部移除；其余（Instant）首帧移除。
    public void Tick(float dt_, IEffectTarget target_)
    {
        for (int i = _actives.Count - 1; i >= 0; i--)
        {
            var effect = _actives[i];
            switch (effect.Effect.EffectType)
            {
                case EEffectType.Instant:
                    effect.OnRemove(target_);
                    _actives.RemoveAt(i);
                    break;
                case EEffectType.Timed:
                    effect.Tick(dt_);
                    if (effect.IsFinished)
                    {
                        effect.OnRemove(target_);
                        _actives.RemoveAt(i);
                    }
                    break;
                case EEffectType.Static:
                    break;
            }
        }
    }
    #endregion

    #region 施加一个效果
    /// <summary>所有类型均在施加时执行一次（属性修正/即时伤害由此生效）；Timed 加入列表由 Tick 计时。source_ 为攻击者来源（环境伤害为 null，伤害类数据走 IEffectTarget.TakeDamage 专用通道）。</summary>
    public void ApplyEffect(SO_BaseEffect effect_, IEffectTarget target_, IEffectSource source_ = null)
    {
        var activeEffect = new ActiveEffect(effect_, source_);
        _actives.Add(activeEffect);
        activeEffect.Execute(target_);
    }
    #endregion

    #region 按照id移除效果
    /// <summary>按效果 id 手动移除（倒序遍历，命中即 OnRemove 回滚并从列表移除全部同名匹配，适配叠加层）。Static 效果由此主动撤除；Instant/Timed 同样可按 id 移除，无副作用。</summary>
    public void RemoveEffect(string id_, IEffectTarget target_)
    {
        for (int i = _actives.Count - 1; i >= 0; i--)
        {
            if (_actives[i].Id == id_)
            {
                _actives[i].OnRemove(target_);
                _actives.RemoveAt(i);
            }
        }
    }
    #endregion
}

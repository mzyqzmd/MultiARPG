using System.Collections.Generic;

/// <summary>效果控制器（普通 C# 类）：持有生效中的效果列表，负责施加与每帧驱动持续效果。</summary>
public class EffectControl
{
    private readonly List<ActiveEffect> _actives = new();

    /// <summary>施加一个效果：所有类型均在施加时执行一次（属性修正/即时伤害由此生效）；Timed 加入列表由 Tick 计时。source_ 为攻击者来源（环境伤害为 null，伤害类数据走 IEffectTarget.TakeDamage 专用通道）。</summary>
    public void ApplyEffect(SO_BaseEffect effect_, IEffectTarget target_, IEffectSource source_ = null)
    {
        var activeEffect = new ActiveEffect(effect_, source_);
        _actives.Add(activeEffect);
        activeEffect.Execute(target_);
    }

    /// <summary>每帧驱动持续效果（仅 server 端调用）。Timed 计时、到期移除（先 OnRemove 回滚）；Static 为挂载态保留至外部移除；其余（Instant）首帧移除。</summary>
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
}

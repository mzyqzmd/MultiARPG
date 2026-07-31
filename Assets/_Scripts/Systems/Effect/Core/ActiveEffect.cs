/// <summary>运行时生效中的效果实例：包装一个 SO_BaseEffect，并维护 Timed 效果的剩余时间。</summary>
public class ActiveEffect
{
    private readonly SO_BaseEffect _effect;
    private readonly IEffectSource _source;
    private float _remaining;

    public SO_BaseEffect Effect => _effect;
    public bool IsFinished => _remaining <= 0f;

    public ActiveEffect(SO_BaseEffect effect_, IEffectSource source_ = null)
    {
        _effect = effect_;
        _source = source_;
        _remaining = effect_.EffectType == EEffectType.Timed ? effect_.Duration : 0f;
    }

    public void Execute(IEffectTarget target_) => _effect.Execute(target_, _source);
    public void OnRemove(IEffectTarget target_) => _effect.OnRemove(target_, _source);
    public void Tick(float dt_) => _remaining -= dt_;
}

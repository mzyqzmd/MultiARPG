/// <summary>运行时生效中的效果实例：包装一个 SO_BaseEffect，并由内嵌 EventTimer 本地驱动周期触发与到期。target_/source_ 在构造时经闭包捕获，EventTimer 零感知效果领域类型。</summary>
public class ActiveEffect
{
    private readonly SO_BaseEffect _effect;
    private readonly IEffectSource _source;
    private readonly string _id;
    private readonly IEffectTarget _target;
    private readonly EffectControl _owner;
    private readonly EventTimer _timer;

    public SO_BaseEffect Effect => _effect;
    public string Id => _id;

    public ActiveEffect(SO_BaseEffect effect_, IEffectTarget target_, IEffectSource source_, EffectControl owner_)
    {
        _effect = effect_;
        _id = effect_.Id;
        _source = source_;
        _target = target_;
        _owner = owner_;
        _timer = new EventTimer(_effect.Duration, _effect.TickInterval);
        _timer.OnStart += () => _effect.Execute(_target, _source);
        _timer.OnInterval += () => _effect.Execute(_target, _source);
        _timer.OnComplete += () =>
        {
            _effect.OnRemove(_target, _source);
            _owner.RemoveActive(this);
        };
        _timer.Start();
    }

    public void Stop() => _timer.Stop();
}

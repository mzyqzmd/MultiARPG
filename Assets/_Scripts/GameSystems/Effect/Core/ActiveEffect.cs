using System;

/// <summary>运行时生效中的效果实例：包装一个 SO_BaseEffect，并由内嵌 EventTimer 本地驱动周期触发与到期。target_/source_ 经 Reset 注入，EventTimer 零感知效果领域类型。自身与内嵌 timer 均来自对象池复用。</summary>
public class ActiveEffect
{
    private readonly PlainObjectPool<EventTimer> _timerPool;
    private BaseEffect _effect;
    private IEffectTarget _target;
    private IEffectSource _source;
    private EventTimer _timer;

    public event Action<ActiveEffect> OnEffectComplete;

    /// <summary>构造供对象池 CreateFunc 使用：仅持有 timer 池引用，具体参数经 Reset 注入。</summary>
    public ActiveEffect(PlainObjectPool<EventTimer> timerPool_)
    {
        _timerPool = timerPool_;
    }

    #region 初始化（池借取后调用）
    public void Init(BaseEffect effect_, IEffectTarget target_, IEffectSource source_)
    {
        _effect = effect_;
        _target = target_;
        _source = source_;
        _timer = _timerPool.Get();
        _timer.Reset(_effect.Duration, _effect.TickInterval);
        _timer.OnStart += OnTimerStart;
        _timer.OnInterval += OnTimerInterval;
        _timer.OnComplete += OnTimerComplete;
        _timer.Start();
    }
    #endregion

    #region 归还（效果结束后由 EffectControl 调用）
    public void Release()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.ClearEvents();
            _timerPool.Release(_timer);
            _timer = null;
        }
        _effect = null;
        _target = null;
        _source = null;
        OnEffectComplete = null;
    }
    #endregion

    #region 计时器回调
    private void OnTimerStart() => _effect.Execute(_target, _source);
    private void OnTimerInterval() => _effect.Execute(_target, _source);
    private void OnTimerComplete()
    {
        //先 OnRemove 回滚效果（如倍率复位），再触发 OnEffectComplete 让 EffectControl 归还池实例。
        //顺序不可颠倒：Release 会清空 _effect=null，若先 Invoke 再 OnRemove 会 NRE。
        _effect.OnRemove(_target, _source);
        OnEffectComplete?.Invoke(this);
    }
    #endregion
}

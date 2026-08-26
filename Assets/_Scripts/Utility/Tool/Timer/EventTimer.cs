using System;

/// <summary>
/// 事件计时器（普通类工具）。经 Start 向 TickService 惰性登记自身，由全局驱动源每帧推进，
/// 调用方无需手动 Tick。对外暴露开始/间隔/结束三类事件，跑满总时长后自动注销。
/// 支持 Reset 重置参数与 ClearEvents 清空订阅，可被对象池复用。
/// </summary>
public class EventTimer 
{
    public event Action OnStart;
    public event Action OnInterval;
    public event Action OnComplete;

    private float _duration;
    private float _interval;
    private float _elapsed;
    private float _intervalTimer;
    private bool _running;
    private bool _registered;

    #region 对象池构造（无参，供 PlainObjectPool CreateFunc 使用，不自动注册）
    /// <summary>无参构造（供对象池 CreateFunc 使用，不自动注册 TickService）。参数与注册经 Reset + Start 惰性注入。</summary>
    public EventTimer() { }
    #endregion

    #region 直接构造（带参，保留原始 API，构造即注册）
    public EventTimer(float duration_, float interval_)
    {
        _duration = duration_;
        _interval = interval_;
        TickService.Instance.Register(Tick);
        _registered = true;
    }
    #endregion

    #region 启动与停止
    public void Start()
    {
        if (_running)
            return;
        _running = true;
        _elapsed = 0f;
        _intervalTimer = 0f;
        RegisterToTick();
        OnStart?.Invoke();
    }

    public void Stop()
    {
        if (!_running)
            return;
        _running = false;
        UnregisterFromTick();
    }

    #endregion

    #region 池化支持
    /// <summary>重置时长/间隔与累计计时，使其可被对象池复用。不改变已订阅的事件委托（由调用方决定），但会确保未运行态。</summary>
    public void Reset(float duration_, float interval_)
    {
        _duration = duration_;
        _interval = interval_;
        _elapsed = 0f;
        _intervalTimer = 0f;
        _running = false;
        UnregisterFromTick();
    }

    /// <summary>清空全部事件订阅，断开对外部 target_/source_ 等闭包捕获，避免归还池后悬空调用与内存泄漏。</summary>
    public void ClearEvents()
    {
        OnStart = null;
        OnInterval = null;
        OnComplete = null;
    }
    #endregion

    #region 每帧推进
    private void Tick(float dt_)
    {
        if (!_running)
            return;
        _elapsed += dt_;
        // 间隔触发（interval<=0 时不触发）
        if (_interval > 0f)
        {
            _intervalTimer += dt_;
            while (_intervalTimer >= _interval)
            {
                _intervalTimer -= _interval;
                OnInterval?.Invoke();
            }
        }
        // 到达总时长：触发结束并自动注销
        if (_elapsed >= _duration)
        {
            _running = false;
            OnComplete?.Invoke();
            UnregisterFromTick();
        }
    }
    #endregion

    #region 注册与注销
    private void RegisterToTick()
    {
        if (_registered)
            return;
        TickService.Instance.Register(Tick);
        _registered = true;
    }

    private void UnregisterFromTick()
    {
        if (!_registered)
            return;
        TickService.Instance.Unregister(Tick);
        _registered = false;
    }
    #endregion
}

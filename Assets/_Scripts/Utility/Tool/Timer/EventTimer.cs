using System;

/// <summary>
/// 事件计时器（普通类工具）。构造后向 TickService 登记自身，由全局驱动源每帧推进，
/// 调用方无需手动 Tick。对外暴露开始/间隔/结束三类事件，跑满总时长后自动注销。
/// </summary>
public class EventTimer 
{
    public event Action OnStart;
    public event Action OnInterval;
    public event Action OnComplete;

    private readonly float _duration;
    private readonly float _interval;
    private float _elapsed;
    private float _intervalTimer;
    private bool _running;
    private bool _registered;

    public EventTimer(float duration_, float interval_)
    {
        _duration = duration_;
        _interval = interval_;
        TickService.Instance.Register(Tick);
        _registered = true;
    }

    #region 启动与停止
    public void Start()
    {
        if (_running)
            return;
        _running = true;
        _elapsed = 0f;
        _intervalTimer = 0f;
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

    #region 注销
    private void UnregisterFromTick()
    {
        if (!_registered)
            return;
        TickService.Instance.Unregister(Tick);
        _registered = false;
    }
    #endregion
}

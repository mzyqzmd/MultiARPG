using System;

/// <summary>极简类型安全事件总线：仅承载少数跨领域"游戏级"信号（事件参数类型定义在 EventBus/Event/ 下，每类型一文件）。
/// 领域事件仍留在各自 Service，避免退化为事件版 Service Locator。每个事件类型对应一个静态分发器，无字符串 key。
/// 事件参数统一用 readonly struct（无字段/仅含引用字段），避免每次 Raise 的堆分配与 GC。</summary>
public static class EventBus<TArgs> where TArgs : struct, IEventBusEvent
{
    private static event Action<TArgs> _handlers;

    /// <summary>订阅事件。</summary>
    public static void Subscribe(Action<TArgs> handler_) => _handlers += handler_;
    /// <summary>取消订阅。</summary>
    public static void Unsubscribe(Action<TArgs> handler_) => _handlers -= handler_;
    /// <summary>发布事件。</summary>
    public static void InvokeEvent(TArgs args_) => _handlers?.Invoke(args_);
    /// <summary>清空所有订阅（用于会话结束，防止跨 play session 残留）。</summary>
    public static void Clear() => _handlers = null;
}

/// <summary>即将返回主菜单（LoadScene 之前）的跨领域信号：订阅方趁仍在世界场景时自组存档；
/// Host 端额外经 ClientRpc 通知所有 Client 各自本地存档。</summary>
public readonly struct ReturningToMainMenuEvent : IEventBusEvent { }

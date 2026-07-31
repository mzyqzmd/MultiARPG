/// <summary>本地玩家已绑定到 UI 服务时的跨领域信号，携带该玩家引用。</summary>
public readonly struct LocalPlayerBoundEvent: IEventBusEvent
{
    public Player Player { get; }
    public LocalPlayerBoundEvent(Player player_) => Player = player_;
}

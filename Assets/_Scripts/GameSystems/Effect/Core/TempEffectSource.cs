/// <summary>
/// 跨网络临时 IEffectSource 实现（普通类）：目标端收到 RPC 传来的 multiplier 后构造此实例，
/// 供 DamageEffect.Execute 读取倍率。攻击者本机的 StatsControl 不可跨网络传递，故用轻量包装。
/// 仅含 DamageMultiplier 字段，每次命中一次微小堆分配（命中频率非每帧，GC 影响 negligible）。
/// </summary>
public class TempEffectSource : IEffectSource
{
    public float DamageMultiplier { get; set; }

    public TempEffectSource(float multiplier_) => DamageMultiplier = multiplier_;
}

/// <summary>效果来源接口：标识「谁发起效果 / 伤害」。除身份外，持有伤害倍率字段（由 DamageMultiplierEffect 在蓄力时临时写入、到期复位）。
/// 实际伤害数值（physical/magic 两类）仍由 DamageEffect 承载，倍率经本接口读取，不与武器手乘耦合。</summary>
public interface IEffectSource
{
    /// <summary>当前伤害倍率，默认 1。DamageMultiplierEffect 施加时写入、到期 OnRemove 复位 1。</summary>
    float DamageMultiplier { get; set; }
}

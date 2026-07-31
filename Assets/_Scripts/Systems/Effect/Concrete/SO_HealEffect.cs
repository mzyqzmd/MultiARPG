using UnityEngine;

/// <summary>即时回复效果：施加即经 IEffectTarget.Heal（目标为 CharacterStatsControl）恢复目标生命。</summary>
[CreateAssetMenu(fileName = "SO_HealEffect", menuName = "MultiARPG/Effects/HealEffect")]
public class SO_HealEffect : SO_BaseEffect
{
    [Tooltip("单次恢复的生命值。")]
    [SerializeField] private int _amount;

    public override void Execute(IEffectTarget target_, IEffectSource _) => target_.Heal(_amount);
}

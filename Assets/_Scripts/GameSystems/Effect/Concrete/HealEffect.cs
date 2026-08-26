using UnityEngine;

public class HealEffect : BaseEffect
{
    [Tooltip("单次恢复的生命值。")]
    [SerializeField] private int _amount;

    public override void Execute(IEffectTarget target_, IEffectSource _) => target_.Heal(_amount);
}

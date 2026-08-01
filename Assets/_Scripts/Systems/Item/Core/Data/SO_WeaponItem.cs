using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SO_WeaponItem : SO_BaseItem
{
    [Header("伤害效果")]
    [Tooltip("武器伤害经此 SO_DamageEffect 资产承载（五类伤害数值配置在该资产内）。")]
    [SerializeField] protected SO_DamageEffect _damageEffect;

    /// <summary>借道 Effect 体系：用武器持有的 SO_DamageEffect 直接作用于目标，source_ 透传攻击者来源。</summary>
    public override void UseItem(IEffectTarget target_, IEffectSource source_) =>
        _damageEffect.Execute(target_, source_);
}

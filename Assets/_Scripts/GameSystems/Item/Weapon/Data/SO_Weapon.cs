using Animancer;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

/// <summary>武器专用数据：在通用物品字段之外，额外承载蓄力重击倍率配置。</summary>
[CreateAssetMenu(fileName = "Weapon", menuName = "MultiARPG/Item/Weapon")]
public class SO_Weapon : SO_BaseItem
{
    [Header("蓄力重击倍率")]
    [Tooltip("短蓄力（达到短阈值但未满）时的伤害倍率，如 1.5 表示 150% 伤害。")]
    [SerializeField] private float _shortChargeMulti = 1.5f;
    [Tooltip("长蓄力（满蓄）时的伤害倍率，如 2.5 表示 250% 伤害。")]
    [SerializeField] private float _longChargeMulti = 2.5f;
    [OdinSerialize] private Dictionary<string, ClipTransition> _weaponAnimations = new();

    /// <summary>短蓄力倍率。</summary>
    public float ShortChargeMulti => _shortChargeMulti;
    /// <summary>长蓄力倍率。</summary>
    public float LongChargeMulti => _longChargeMulti;
    /// <summary>武器动作。</summary>
    public Dictionary<string, ClipTransition> WeaponAnimations => _weaponAnimations;
}

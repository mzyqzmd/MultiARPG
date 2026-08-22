using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BossZombieStats", menuName = "MultiARPG/Stats/NPC/Enemy/BossZombieStats")]
public class SO_BossZombieStats : SO_CharacterStats
{
    [Header("僵尸Boss配置")]
    [SerializeField] private float _runSpeed = 5;
    [SerializeField] private int _attackDamage = 20;
    [Tooltip("累计受到多少次伤害后进入受击打断免疫窗口（期间仍可扣血但不被打断攻击）。")]
    [SerializeField] private int _hitStunThreshold = 4;

    [Header("攻击时序（秒，全部攻击共用）")]
    [SerializeField] private float _attackWindup = 0.2f;
    [SerializeField] private float _attackWindow = 0.9f;
    [SerializeField] private float _attackDuration = 2.2f;

    [Header("各攻击动作攻击距离（米）")]
    [SerializeField] private float _attack01Range = 2.2f;
    [SerializeField] private float _attack02Range = 2.2f;
    [SerializeField] private float _attack03Range = 2.2f;
    [SerializeField] private float _dashAtkRange = 6f;
    [SerializeField] private float _doubleHandRange = 2.5f;
    [SerializeField] private float _footLeftRange = 2.5f;
    [SerializeField] private float _footRightRange = 2.5f;
    [SerializeField] private float _groundRange = 3f;

    public float RunSpeed => _runSpeed;
    public int AttackDamage => _attackDamage;
    public int HitStunThreshold => _hitStunThreshold;
    public float AttackWindup => _attackWindup;
    public float AttackWindow => _attackWindow;
    public float AttackDuration => _attackDuration;

    public float GetAttackRange(EBossZombieAttackAction action_) => action_ switch
    {
        EBossZombieAttackAction.Attack01 => _attack01Range,
        EBossZombieAttackAction.Attack02 => _attack02Range,
        EBossZombieAttackAction.Attack03 => _attack03Range,
        EBossZombieAttackAction.DashAtk => _dashAtkRange,
        EBossZombieAttackAction.DoubleHand => _doubleHandRange,
        EBossZombieAttackAction.FootLeft => _footLeftRange,
        EBossZombieAttackAction.FootRight => _footRightRange,
        EBossZombieAttackAction.Ground => _groundRange,
        _ => _attack01Range
    };
}

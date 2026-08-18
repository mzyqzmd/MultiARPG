using UnityEngine;

[CreateAssetMenu(fileName = "Zombie", menuName = "MultiARPG/Stats/ZombieStats")]
public class SO_ZombieStats : SO_CharacterStats
{
    [Header("僵尸配置")]
    [SerializeField] private float _runSpeed = 5;
    [SerializeField] private int _attackDamage = 10;
    [SerializeField] private float _attackRange = 1.8f;
    [Tooltip("累计受到多少次伤害后进入受击打断免疫窗口（期间仍可扣血但不被打断攻击）。由 ZombieHitStun 效果资产控制窗口时长。")]
    [SerializeField] private int _hitStunThreshold = 3;
    [Header("攻击时序（秒）")]
    [SerializeField] private float _attackWindup = 0.15f;
    [SerializeField] private float _attackWindow = 0.9f;
    [SerializeField] private float _attackDuration = 2.1f;
    public float RunSpeed => _runSpeed;
    public int AttackDamage => _attackDamage;
    public float AttackRange => _attackRange;
    public int HitStunThreshold => _hitStunThreshold;
    public float AttackWindup => _attackWindup;
    public float AttackWindow => _attackWindow;
    public float AttackDuration => _attackDuration;
}

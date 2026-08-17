using UnityEngine;

[CreateAssetMenu(fileName = "NormalZombie", menuName = "MultiARPG/Stats/NormalZombieStats")]
public class SO_ZombieStats : SO_CharacterStats
{
    [Header("僵尸配置")]
    [SerializeField] private float _runSpeed = 5;
    [SerializeField] private int _attackDamage = 10;
    [SerializeField] private float _attackRange = 1.8f;
    [Header("攻击时序（秒）")]
    [SerializeField] private float _attackWindup = 0.15f;
    [SerializeField] private float _attackWindow = 0.9f;
    [SerializeField] private float _attackDuration = 2.1f;
    public float RunSpeed => _runSpeed;
    public int AttackDamage => _attackDamage;
    public float AttackRange => _attackRange;
    public float AttackWindup => _attackWindup;
    public float AttackWindow => _attackWindow;
    public float AttackDuration => _attackDuration;
}

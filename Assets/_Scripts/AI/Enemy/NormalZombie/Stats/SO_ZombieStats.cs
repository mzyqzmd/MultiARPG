using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NormalZombie", menuName = "MultiARPG/Stats/NormalZombieStats")]
public class SO_ZombieStats : SO_CharacterStats
{
    [Header("僵尸配置")]
    [SerializeField] private float _runSpeed = 5;
    [SerializeField] private int _attackDamage = 10;
    [SerializeField] private float _attackRange = 1.8f;
    public float RunSpeed => _runSpeed;
    public int AttackDamage => _attackDamage;
    public float AttackRange => _attackRange;
}

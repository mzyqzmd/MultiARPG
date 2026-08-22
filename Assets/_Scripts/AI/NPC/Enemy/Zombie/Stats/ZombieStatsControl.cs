using UnityEngine;

public class ZombieStatsControl : CharacterStatsControl
{
    [Header("僵尸引用")]
    [SerializeField] private Zombie _zombie;

    private SO_ZombieStats ZombieStats => _characterStats as SO_ZombieStats;

    public override float GetMoveSpeed(bool running_) =>
        running_ ? ZombieStats.RunSpeed : ZombieStats.NormalMoveSpeed;

    public int AttackDamage => ZombieStats.AttackDamage;
    public float AttackRange => ZombieStats.AttackRange;
    public int HitStunThreshold => ZombieStats.HitStunThreshold;
    public float AttackWindup => ZombieStats.AttackWindup;
    public float AttackWindow => ZombieStats.AttackWindow;
    public float AttackDuration => ZombieStats.AttackDuration;
}

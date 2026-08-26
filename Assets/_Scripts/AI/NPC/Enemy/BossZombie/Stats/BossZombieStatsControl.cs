using UnityEngine;

public class BossZombieStatsControl : CharacterStatsControl
{
    [SerializeField] private BossZombie _bossZombie;
    private SO_BossZombieStats BossZombieStats => _characterStats as SO_BossZombieStats;

    public override float GetMoveSpeed(bool running_) =>
       running_ ? BossZombieStats.RunSpeed : BossZombieStats.NormalMoveSpeed;

    public float AttackDuration => BossZombieStats.AttackDuration;

    public float GetAttackRange(EBossZombieAttackAction action_) =>
        BossZombieStats.GetAttackRange(action_);
}

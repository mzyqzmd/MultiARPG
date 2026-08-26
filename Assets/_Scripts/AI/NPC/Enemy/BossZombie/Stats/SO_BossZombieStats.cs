using UnityEngine;

[CreateAssetMenu(fileName = "BossZombieStats", menuName = "MultiARPG/Stats/NPC/Enemy/BossZombieStats")]
public class SO_BossZombieStats : SO_CharacterStats
{
    [Header("僵尸Boss配置")]
    [SerializeField] private float _runSpeed = 5;

    [Header("攻击时序（秒，全部攻击共用）")]
    [SerializeField] private float _attackDuration = 3.5f;

    [Header("各攻击动作攻击距离（米）")]
    [SerializeField] private float _shortAttackRange = 4f;
    [SerializeField] private float _longAttackRange = 8f;

    public float RunSpeed => _runSpeed;
    public float AttackDuration => _attackDuration;

    public float GetAttackRange(EBossZombieAttackAction action_)
    {
        return action_ switch
        {
            EBossZombieAttackAction.DashAtk => _longAttackRange,
            EBossZombieAttackAction.Ground => _longAttackRange,
            _ => _shortAttackRange,
        };
    }
}

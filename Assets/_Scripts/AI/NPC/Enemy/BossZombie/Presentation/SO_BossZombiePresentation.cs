using UnityEngine;

[CreateAssetMenu(fileName = "BossZombiePresentation", menuName = "MultiARPG/Presentation/NPC/Enemy/BossZombiePresentation")]
public class SO_BossZombiePresentation : SO_CharacterPresentation
{
    [Header("idle 音")]
    [SerializeField] private string _idleAudioGroup = "BossZombie";
    [SerializeField] private string _idleAudioKey = "Idle";

    [Header("死亡音")]
    [SerializeField] private string _deathAudioGroup = "BossZombie";
    [SerializeField] private string _deathAudioKey = "Die";

    [Header("攻击音")]
    [SerializeField] private string _attackAudioGroup = "BossZombie";
    [SerializeField] private string _attackAudioKey = "Attack";

    public string IdleAudioGroup => _idleAudioGroup;
    public string IdleAudioKey => _idleAudioKey;
    public string DeathAudioGroup => _deathAudioGroup;
    public string DeathAudioKey => _deathAudioKey;
    public string AttackAudioGroup => _attackAudioGroup;
    public string AttackAudioKey => _attackAudioKey;
}

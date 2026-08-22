using UnityEngine;

/// <summary>
/// 僵尸表现配置：继承基类（受击 VFX/受伤音/血溅音/idle 音），扩展死亡音与攻击音。
/// 僵尸所有音效统一走 "Zombie" 音频组（由 Inspector 配置具体组名/音效名）。
/// </summary>
[CreateAssetMenu(fileName = "ZombiePresentation", menuName = "MultiARPG/Presentation/NPC/Enemy/ZombiePresentation")]
public class SO_ZombiePresentation : SO_CharacterPresentation
{
    [Header("idle 音")]
    [Tooltip("待机循环音的音频组名。")]
    [SerializeField] private string _idleAudioGroup = "Zombie";
    [Tooltip("待机循环音的音效名。")]
    [SerializeField] private string _idleAudioKey = "Idle";

    [Header("死亡音")]
    [Tooltip("死亡音的音频组名。")]
    [SerializeField] private string _deathAudioGroup = "Zombie";
    [Tooltip("死亡音的音效名。")]
    [SerializeField] private string _deathAudioKey = "Die";

    [Header("攻击音")]
    [Tooltip("攻击音的音频组名。")]
    [SerializeField] private string _attackAudioGroup = "Zombie";
    [Tooltip("攻击音的音效名。")]
    [SerializeField] private string _attackAudioKey = "Attack";

    public string IdleAudioGroup => _idleAudioGroup;
    public string IdleAudioKey => _idleAudioKey;
    public string DeathAudioGroup => _deathAudioGroup;
    public string DeathAudioKey => _deathAudioKey;
    public string AttackAudioGroup => _attackAudioGroup;
    public string AttackAudioKey => _attackAudioKey;
}

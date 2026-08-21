using UnityEngine;

/// <summary>
/// 玩家表现配置：继承基类（受击 VFX/受伤音/血溅音/idle 音），扩展死亡音与攻击音。
/// </summary>
[CreateAssetMenu(fileName = "PlayerPresentation", menuName = "MultiARPG/Presentation/PlayerPresentation")]
public class SO_PlayerPresentation : SO_CharacterPresentation
{
    [Header("idle 音")]
    [Tooltip("待机循环音的音频组名。")]
    [SerializeField] private string _idleAudioGroup = "Player";
    [Tooltip("待机循环音的音效名。")]
    [SerializeField] private string _idleAudioKey = "Idle";

    [Header("死亡音")]
    [Tooltip("死亡音的音频组名。")]
    [SerializeField] private string _deathAudioGroup = "Player";
    [Tooltip("死亡音的音效名。")]
    [SerializeField] private string _deathAudioKey = "Die";

    [Header("攻击音")]
    [Tooltip("攻击音的音频组名。")]
    [SerializeField] private string _attackAudioGroup = "Player";
    [Tooltip("攻击音的音效名。")]
    [SerializeField] private string _attackAudioKey = "Attack";

    [Header("翻滚音")]
    [Tooltip("翻滚音的音频组名。")]
    [SerializeField] private string _dodgeAudioGroup = "Player";
    [Tooltip("翻滚音的音效名。")]
    [SerializeField] private string _dodgeAudioKey = "Dodge";

    [Header("力竭呼吸音")]
    [Tooltip("力竭呼吸音的音频组名。")]
    [SerializeField] private string _exhaustedAudioGroup = "Player";
    [Tooltip("力竭呼吸音的音效名。")]
    [SerializeField] private string _exhaustedAudioKey = "Exhausted";

    public string IdleAudioGroup => _idleAudioGroup;
    public string IdleAudioKey => _idleAudioKey;
    public string DeathAudioGroup => _deathAudioGroup;
    public string DeathAudioKey => _deathAudioKey;
    public string AttackAudioGroup => _attackAudioGroup;
    public string AttackAudioKey => _attackAudioKey;
    public string DodgeAudioGroup => _dodgeAudioGroup;
    public string DodgeAudioKey => _dodgeAudioKey;
    public string ExhaustedAudioGroup => _exhaustedAudioGroup;
    public string ExhaustedAudioKey => _exhaustedAudioKey;
}

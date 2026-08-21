using UnityEngine;

/// <summary>
/// 角色表现配置（所有角色类型共有）：受击飙血 VFX、受击受伤音、受击血溅音、idle 音的数据库 key。
/// 死亡音 / 攻击音为具体子类专属（Player/Zombie 有、FakeMan 无），由子类表现配置扩展。
/// key 字符串对应 SO_AudioDatabase（组名+音效名）与 SO_VFXDatabase（特效名），具体资产由 Inspector 配置。
/// </summary>
public abstract class SO_CharacterPresentation : ScriptableObject
{
    [Header("受击飙血特效")]
    [Tooltip("受击时播放的飙血 VFX 名（对应 SO_VFXDatabase 条目）。")]
    [SerializeField] private string _hitVfxKey = "FX_BloodSplatter";

    [Header("受击血溅音")]
    [Tooltip("受击血溅音的音频组名。")]
    [SerializeField] private string _hitSplatterGroup = "General";
    [Tooltip("受击血溅音的音效名。")]
    [SerializeField] private string _hitSplatterKey = "BloodSplatter";

    [Header("受击受伤音")]
    [Tooltip("受击受伤音的音频组名（对应 SO_AudioDatabase 的 group）。")]
    [SerializeField] private string _hitAudioGroup = "Player";
    [Tooltip("受击受伤音的音效名（组内 name）。")]
    [SerializeField] private string _hitAudioKey = "Hit";

    public string HitVFXKey => _hitVfxKey;
    public string HitAudioGroup => _hitAudioGroup;
    public string HitAudioKey => _hitAudioKey;
    public string HitSplatterGroup => _hitSplatterGroup;
    public string HitSplatterKey => _hitSplatterKey;
}

/// <summary>允许运行时被效果修正的属性白名单。未列出的属性（如 RotationSpeed）保持配置透传、不可修改。</summary>
public enum EStatType
{
    NormalMoveSpeed,
    MaxHealth,
    RunSpeed,
    DodgeSpeed,
    MaxStamina,
    StaminaRegenRate,
    HitDuration,
}

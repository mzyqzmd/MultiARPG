using UnityEngine;

/// <summary>
/// 玩家基础数值配置（ScriptableObject）：在角色通用数值（SO_CharacterStats）基础上扩展闪避与耐力参数。
/// 资产实例经 Player.prefab 的 _soPlayerStats 字段注入，由 PlayerStatsControl 透传。
/// </summary>
[CreateAssetMenu(fileName = "SO_PlayerStats", menuName = "MultiARPG/Stats/PlayerStats")]
public class SO_PlayerStats : SO_CharacterStats
{
    [Header("跑步参数")]
    [Tooltip("跑步速度（米/秒）。按住跑步键（键盘左Shift / 手柄摇杆≥0.7）时的移动速度。")]
    [SerializeField] private float _runSpeed = 5f;

    [Header("闪避参数")]
    [Tooltip("闪避爆发位移速度（米/秒）。决定闪避一段的横向冲刺快慢。")]
    [SerializeField] private float _dodgeSpeed = 12f;
    [Tooltip("闪避持续时间（秒）。同时与动画 DodgeAnimSpeed 对齐，使动画恰好在该时长内播完。")]
    [SerializeField] private float _dodgeDuration = 0.4f;

    [Header("跳跃参数")]
    [Tooltip("跳跃起跳高度（米）。经物理公式 sqrt(h * -2 * g) 换算为起跳初速度，越大跳得越高。")]
    [SerializeField] private float _jumpHeight = 1.5f;
    [Tooltip("重力加速度（负值，米/秒²）。仅作用于垂直运动，绝对值越大下落越快。")]
    [SerializeField] private float _gravity = -25f;
    [Tooltip("主动跳跃一次性消耗的耐力值。耐力不足时跳跃请求被丢弃，无法起跳。")]
    [SerializeField] private int _jumpStaminaCost = 20;

    [Header("轻击参数")]
    [Tooltip("单次轻击一次性消耗的耐力值。耐力不足时轻击请求被丢弃，无法挥砍。")]
    [SerializeField] private int _normalAttackStaminaCost = 15;
    [Tooltip("轻击动画持续时间（秒）。同时与动画 NormalAttackSpeed 对齐，使动画恰好在该时长内播完。")]
    [SerializeField] private float _normalAttackDuration = 0.8f;

    [Header("重击(蓄力)参数")]
    [Tooltip("单次重击一次性消耗的耐力值。耐力不足时重击请求被丢弃，无法蓄力。")]
    [SerializeField] private int _heavyAttackStaminaCost = 25;

    [Header("击晕参数")]
    [Tooltip("击晕时长（秒）。玩家被击晕后停留在击晕态的时长（不可被闪避打断），可被延长/缩短击晕类效果经 EStatType.StunDuration 运行时修正。")]
    [SerializeField] private float _stunDuration = 1.5f;

    [Header("复活参数")]
    [Tooltip("复活倒计时（秒）。玩家死亡后停留在死亡态的时长，到点自动满血复活到最近触碰过的存档点。")]
    [SerializeField] private float _respawnDuration = 3f;

    [Header("耐力参数")]
    [Tooltip("耐力上限。用于跑步 / 闪避 / 跳跃等消耗性动作的通用资源。")]
    [SerializeField] private int _maxStamina = 100;
    [Tooltip("单次闪避消耗的耐力值。")]
    [SerializeField] private int _dodgeStaminaCost = 20;
    [Tooltip("耐力回复速率（点/秒）。停止消耗并经回复延迟后开始自动回复。")]
    [SerializeField] private int _staminaRegenRate = 20;
    [Tooltip("耐力回复延迟（秒）。停止消耗耐力后需等待该时长才开始自动回复。")]
    [SerializeField] private float _staminaRegenDelay = 1f;
    [Tooltip("跑步时每秒持续消耗的耐力值。")]
    [SerializeField] private int _runStaminaDrain = 10;

    public float RunSpeed => _runSpeed;

    public float DodgeSpeed => _dodgeSpeed;
    public float DodgeDuration => _dodgeDuration;

    public float JumpHeight => _jumpHeight;
    public float Gravity => _gravity;
    public int JumpStaminaCost => _jumpStaminaCost;
    public int NormalAttackStaminaCost => _normalAttackStaminaCost;
    public float NormalAttackDuration => _normalAttackDuration;
    public int HeavyAttackStaminaCost => _heavyAttackStaminaCost;

    public float StunDuration => _stunDuration;
    /// <summary>复活倒计时（秒）：不被任何效果修正，保持配置原值透传。</summary>
    public float RespawnDuration => _respawnDuration;

    public int MaxStamina => _maxStamina;
    public int DodgeStaminaCost => _dodgeStaminaCost;
    public int StaminaRegenRate => _staminaRegenRate;
    public float StaminaRegenDelay => _staminaRegenDelay;
    public int RunStaminaDrain => _runStaminaDrain;
}

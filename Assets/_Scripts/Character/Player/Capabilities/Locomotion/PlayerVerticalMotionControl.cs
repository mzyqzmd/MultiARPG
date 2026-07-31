using UnityEngine;

/// <summary>
/// 玩家垂直运动控制核心：统一收敛跳跃/坠落/地面下压的垂直速度、重力与落地判定。
/// Jump 与 Fall 两态均经它访问，避免重复逻辑。非 MonoBehaviour，构造函数注入依赖。
/// </summary>
public class PlayerVerticalMotionControl
{
    private readonly PlayerStatsControl _stats;
    private readonly PlayerGroundCheck _groundCheck;
    private Vector3 _verticalVelocity;

    public bool CanJump => _groundCheck.IsGrounded;
    public bool IsGrounded => _groundCheck.IsGrounded;
    public float JumpHeight => _stats.JumpHeight;
    public float Gravity => _stats.Gravity;
    public Vector3 VerticalVelocity => _verticalVelocity;

    public PlayerVerticalMotionControl(PlayerStatsControl stats_, PlayerGroundCheck groundCheck_)
    {
        _stats = stats_;
        _groundCheck = groundCheck_;
    }

    /// <summary>赋跳跃初速度（向上，由 StatsControl 按跳跃高度与重力推导）。</summary>
    public void Launch() => _verticalVelocity.y = _stats.GetJumpInitialVelocity();
    /// <summary>空中每帧累加重力。</summary>
    public void TickGravity(float dt_) => _verticalVelocity.y += Gravity * dt_;
    /// <summary>地面下压（防 isGrounded 抖动）。</summary>
    public void SnapToGround() => _verticalVelocity.y = -0.1f;
    /// <summary>落地清零垂直速度。</summary>
    public void Reset() => _verticalVelocity = Vector3.zero;
    /// <summary>落地判定（Jump/Fall 共用）：已触地且垂直速度朝下。</summary>
    public bool HasLanded() => _groundCheck.IsGrounded && _verticalVelocity.y <= 0f;
}

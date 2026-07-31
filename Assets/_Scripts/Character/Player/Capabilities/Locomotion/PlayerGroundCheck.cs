using UnityEngine;

/// <summary>
/// 地面检测能力：基于 CharacterController.isGrounded（O(1) 读属性，无射线）。
/// 被 PlayerVerticalMotionControl 持有，供跳跃/坠落落地判定使用。
/// </summary>
public class PlayerGroundCheck
{
    private readonly CharacterController _controller;
    public bool IsGrounded => _controller.isGrounded;
    public PlayerGroundCheck(CharacterController controller_) => _controller = controller_;
}

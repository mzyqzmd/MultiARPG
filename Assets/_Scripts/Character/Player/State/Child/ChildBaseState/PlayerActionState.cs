using UnityEngine;


/// <summary>
/// 子层(动作层)状态基类：在某一姿态内运行的"当前动作"(移动/闪避/攻击...)。
/// 移动/转向/方向计算等原子能力下沉到本基类，具体策略(strafe/自由、速度)由各动作态与父层姿态决定。
/// 依赖(输入/相机/角色控制器/属性/动画)均以构造函数注入。
/// </summary>
public abstract class PlayerActionState : PlayerBaseState<PlayerActionState>
{
    protected CharacterController _characterController;
    protected PlayerActionState(StateMachine<PlayerActionState> stateMachine_,
        Player player_, CharacterController characterController_) : base(stateMachine_, player_)
    {
        _characterController = characterController_;
    }


    #region 由相机朝向与输入向量算出水平移动方向（过渡期用缓存锁定朝向，否则实时）
    protected Vector3 GetMoveDirection(Vector2 input_)
    {
        Vector3 moveDir = _player.PlayerCameraService.MoveForward * input_.y +
            _player.PlayerCameraService.MoveRight * input_.x;
        moveDir.y = 0;
        return moveDir.normalized;
    }
    #endregion

    #region 按给定速度向量位移
    protected void Move(Vector3 velocity_) => _characterController.Move(velocity_ * Time.deltaTime);
    #endregion

    #region 转向：锁定时严格朝向目标；strafe(战斗)面向相机前方；自由面向移动方向
    protected void RotateTowards(Vector3 moveDir_, bool strafe_)
    {
        Transform playerTransform = _characterController.transform;
        Quaternion targetRotation;
        var lockTarget = _player.PlayerCameraService.CurrentLockTarget;
        if (lockTarget != null && lockTarget.LockCenter != null)
        {
            Vector3 toTarget = lockTarget.LockCenter.position - playerTransform.position;
            toTarget.y = 0f;
            targetRotation = toTarget == Vector3.zero ? playerTransform.rotation : Quaternion.LookRotation(toTarget);
        }
        else if (strafe_)
        {
            Vector3 camFwd = _player.PlayerCameraService.MoveForward;
            targetRotation = camFwd == Vector3.zero ? playerTransform.rotation : Quaternion.LookRotation(camFwd);
        }
        else
        {
            targetRotation = Quaternion.LookRotation(moveDir_);
        }
        float maxDegrees = _player.StatsControl.RotationSpeed * Time.deltaTime;
        playerTransform.rotation = Quaternion.RotateTowards(playerTransform.rotation, targetRotation, maxDegrees);
    }
    #endregion

    #region 每帧统一用 GroundCheck 真实检测驱动 IsGrounded 动画参数（单一数据源，避免各状态硬编码）
    public override void Update()
    {
        base.Update();
        _player.Anim.SetBool("IsGrounded", _player.StateControl.IsGrounded);
    }
    #endregion
}

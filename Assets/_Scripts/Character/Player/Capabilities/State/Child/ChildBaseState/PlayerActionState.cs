using UnityEngine;


/// <summary>
/// 子层(动作层)状态基类：在某一姿态内运行的"当前动作"(移动/闪避/攻击...)。
/// 移动/转向/方向计算等原子能力下沉到本基类，具体策略(strafe/自由、速度)由各动作态与父层姿态决定。
/// 依赖(输入/相机/角色控制器/属性/动画)均以构造函数注入。
/// </summary>
public abstract class PlayerActionState : PlayerBaseState<PlayerActionState>
{
    protected readonly CharacterController _characterController;
    protected readonly PlayerStatsControl _statsControl;
    protected readonly Animator _anim;
    protected readonly InputService _inputService;
    protected readonly PlayerCameraService _playerCameraService;
    protected readonly AudioService _audioService;

    protected PlayerActionState(StateMachine<PlayerActionState> stateMachine_,
        PlayerStateControl playerState_, CharacterController characterController_,
        PlayerStatsControl playerStats_, Animator anim_, InputService inputService_,
        PlayerCameraService playerCameraService_, AudioService audioService_) :
        base(stateMachine_, playerState_)
    {
        _characterController = characterController_;
        _statsControl = playerStats_;
        _anim = anim_;
        _inputService = inputService_;
        _playerCameraService = playerCameraService_;
        _audioService = audioService_;
    }

    #region 由相机朝向与输入向量算出水平移动方向
    protected Vector3 GetMoveDirection(Vector2 input_)
    {
        Transform camTransform = _playerCameraService.PlayerCamera.transform;
        Vector3 moveDir = camTransform.forward * input_.y + camTransform.right * input_.x;
        moveDir.y = 0;
        return moveDir.normalized;
    }
    #endregion

    #region 按给定速度向量位移
    protected void Move(Vector3 velocity_) => _characterController.Move(velocity_ * Time.deltaTime);
    #endregion

    #region 转向：strafe(战斗)面向相机前方；自由面向移动方向
    protected void RotateTowards(Vector3 moveDir_, bool strafe_)
    {
        Transform playerTransform = _characterController.transform;
        Quaternion targetRotation;
        if (strafe_)
        {
            Vector3 camFwd = _playerCameraService.PlayerCamera.transform.forward;
            camFwd.y = 0;
            targetRotation = camFwd == Vector3.zero ? playerTransform.rotation : Quaternion.LookRotation(camFwd);
        }
        else
        {
            targetRotation = Quaternion.LookRotation(moveDir_);
        }
        float maxDegrees = _statsControl.GetRotationDelta();
        playerTransform.rotation = Quaternion.RotateTowards(playerTransform.rotation, targetRotation, maxDegrees);
    }
    #endregion

    #region 每帧统一用 GroundCheck 真实检测驱动 IsGrounded 动画参数（单一数据源，避免各状态硬编码）
    public override void Update()
    {
        base.Update();
        _anim.SetBool("IsGrounded", _playerStateControl.IsGrounded);
    }
    #endregion
}

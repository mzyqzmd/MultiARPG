using System;
using UnityEngine;

/// <summary>装备切换动画控制（MonoBehaviour，挂 Player 预制体）：仅持有 Animator，负责播"拔出/收起"动画并暴露两个 Animation Event 出口，
/// 不含任何业务逻辑（物品/装备/状态机/物品服务均不依赖），全部经 Player 桥接编排。</summary>
public class PlayerAnimationEventComponent : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    /// <summary>动画播放完成事件（由 Animation Clip 上的 AnimFinished Event 触发，参数为状态名）。</summary>
    public event Action OnAnimationFinished;

    /// <summary>切换落点帧事件（由 Animation Clip 上的 ToggleFrame Event 触发）：Player 订阅后在此时机提交 Equip/Unequip 挂载。</summary>
    public event Action OnToggleFrame;

    /// <summary>播放拔刀/收刀动画（不区分两者，共用同一 clip）。</summary>
    public void Equip1HandWeapon() => _animator.SetTrigger("Equip1HandWeaponTrigger");

    /// <summary>Animation Clip 事件：动画播完，转发 OnAnimationFinished。</summary>
    public void OnAnimFinished() => OnAnimationFinished?.Invoke();

    /// <summary>Animation Clip 事件：切换落点帧，转发 OnToggleFrame。</summary>
    public void OnToggleFrameEvent() => OnToggleFrame?.Invoke();
}

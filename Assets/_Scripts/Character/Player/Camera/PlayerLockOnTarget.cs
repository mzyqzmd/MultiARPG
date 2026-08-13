using Cinemachine;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 玩家锁定目标组件（MonoBehaviour，挂 Player.prefab）。
/// 职责：检测半径内且视线无遮挡、并实现 ILockableTarget 的对象并锁定；
/// 锁定期间不接管相机视角（Look 输入保持可用，玩家可自由旋转观察）；仅将目标写入 PlayerCameraService.CurrentLockTarget，
/// 由状态层把玩家身体朝向目标。再触发时切到下一个候选；目标销毁/超距/被墙遮挡时自动解除。
/// 仅经 OnLockStateChanged(bool) 通知外部设战斗姿态，零反向依赖状态机。
/// </summary>
public class PlayerLockOnTarget : MonoBehaviour
{
    [Header("玩家引用")]
    [SerializeField] private Player _player;
    /// <summary>锁定状态变化事件（true=已锁定，false=已解除），由 PlayerStateControl 经 [SerializeField] 自订阅设 CombatLocked。</summary>
    public event Action<bool> OnLockStateChanged;

    [Header("检测参数")]
    [SerializeField] private Transform _checkStartPoint;
    [SerializeField] private float _lockRadius = 15f;
    [SerializeField] private LayerMask _blockingLayers;
    [SerializeField] private float _frontFovHalfAngle = 90f;
    [SerializeField] private float _lostSightTimeout = 5f;

    private ILockableTarget _currentTarget;
    private float _lostSightTimer;
    private readonly Queue<ILockableTarget> _excludedTargets = new();

    /// <summary>检测原点：未指定时回退到组件自身 Transform。</summary>
    private Transform CheckOrigin => _checkStartPoint != null ? _checkStartPoint : transform;

    #region 输入事件绑定（由 Player.OnNetworkSpawn/OnNetworkDespawn 在 IsOwner 可靠后转发调用）
    /// <summary>订阅本地玩家锁定目标触发事件（仅 owner 调用）。</summary>
    public void BindInput() => _player.InputService.OnLockOnTargetTriggered += ToggleLock;

    /// <summary>反订阅锁定目标触发事件（与 BindInput 配对，防重 spawn 累积）。</summary>
    public void UnbindInput() => _player.InputService.OnLockOnTargetTriggered -= ToggleLock;
    #endregion

    #region 每帧：失效校验 + 轴驱动
    private void Update()
    {
        if (_currentTarget == null) return;
        bool lostSight = !IsTargetValid(_currentTarget) || !IsTargetInFront(_currentTarget);
        if (lostSight)
        {
            _lostSightTimer += Time.deltaTime;
            if (_lostSightTimer >= _lostSightTimeout) Unlock();
        }
        else
            _lostSightTimer = 0f;
    }
    #endregion

    #region 触发与切换
    /// <summary>输入触发：无目标→锁定半径内最近且未被遮挡候选；有目标→切到下一个候选（无则保持）。</summary>
    private void ToggleLock()
    {
        if (_currentTarget == null)
        {
            var first = FindBestTarget(null);
            if (first != null) LockTo(first);
        }
        else
        {
            // 优先找队列外的其他有效目标（最近优先）；找不到才出队逐个校验
            var next = FindBestTarget(_currentTarget, _excludedTargets);
            if (next == null) next = FindFromExcludedQueue();
            if (next != null) LockTo(next);
            // 无下一个候选则保持当前目标
        }
    }

    /// <summary>锁定到指定目标：标记锁定 + 通知状态机 + 写入相机服务（供玩家朝向目标）；不接管相机视角，Look 输入保持可用。</summary>
    private void LockTo(ILockableTarget target_)
    {
        _currentTarget = target_;
        _lostSightTimer = 0f;
        if (_player.PlayerCameraService != null)
            _player.PlayerCameraService.CurrentLockTarget = target_;
        _excludedTargets.Enqueue(target_);
        OnLockStateChanged?.Invoke(true);
    }

    /// <summary>解除锁定：清标记 + 通知 + 清空相机服务目标引用。</summary>
    private void Unlock()
    {
        _currentTarget = null;
        _lostSightTimer = 0f;
        if (_player.PlayerCameraService != null)
            _player.PlayerCameraService.CurrentLockTarget = null;
        _excludedTargets.Clear();
        OnLockStateChanged?.Invoke(false);
    }

    /// <summary>外部强制解锁入口（动作破锁用）：复用 Unlock 全部逻辑（清目标 + FreeLook 过渡相机）。</summary>
    public void ForceUnlock() => Unlock();
    #endregion

    #region 目标检测
    /// <summary>在 OverlapSphere 候选里按"距离最近且视线无遮挡且≠排除项"选目标；无则返回 null。
    /// excludeQueue_ 非空时，额外排除队列内全部目标（优先锁定队列外的新目标）。</summary>
    private ILockableTarget FindBestTarget(ILockableTarget exclude_, IEnumerable<ILockableTarget> excludeQueue_ = null)
    {
        ILockableTarget best = null;
        var bestDist = float.MaxValue;
        var center = CheckOrigin.position;
        var hits = Physics.OverlapSphere(center, _lockRadius);
        foreach (var hit in hits)
        {
            var target = hit.GetComponent<ILockableTarget>();
            if (target == null || !target.CanBeLocked) continue;
            if (target == exclude_) continue;
            if (excludeQueue_ != null)
            {
                var skipped = false;
                foreach (var q in excludeQueue_)
                    if (q == target) { skipped = true; break; }
                if (skipped) continue;
            }
            if (Physics.Linecast(center, target.LockCenter.position, _blockingLayers)) continue;
            var dist = Vector3.Distance(center, target.LockCenter.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = target;
            }
        }
        return best;
    }

    /// <summary>从排除队列取有效目标：出队校验非空/!=当前/在范围内，不符则丢弃继续；队空返回 null。</summary>
    private ILockableTarget FindFromExcludedQueue()
    {
        while (_excludedTargets.Count > 0)
        {
            var t = _excludedTargets.Dequeue();
            if (t != null && t != _currentTarget && IsTargetValid(t)) return t;
        }
        return null;
    }

    /// <summary>当前目标是否仍有效（未销毁/未失活/未超距/视线无遮挡）。</summary>
    private bool IsTargetValid(ILockableTarget target_)
    {
        if (target_ == null || target_.LockCenter == null) return false;
        if (!target_.CanBeLocked) return false;
        if (Vector3.Distance(CheckOrigin.position, target_.LockCenter.position) > _lockRadius) return false;
        if (Physics.Linecast(CheckOrigin.position, target_.LockCenter.position, _blockingLayers)) return false;
        return true;
    }

    /// <summary>目标是否落在相机前方 180° 扇形内（以 camera.forward 为基准，含俯仰）。</summary>
    private bool IsTargetInFront(ILockableTarget target_)
    {
        if (target_?.LockCenter == null || _player.PlayerCameraService?.PlayerCamera == null) return false;
        var toTarget = (target_.LockCenter.position - CheckOrigin.position).normalized;
        var camForward = _player.PlayerCameraService.PlayerCamera.transform.forward;
        return Vector3.Angle(camForward, toTarget) <= _frontFovHalfAngle;
    }
    #endregion

    #region 调试可视化
    private void OnDrawGizmos()
    {
        var center = CheckOrigin.position;
        // 检测球范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, _lockRadius);
        // 相机前方 180° 扇形边界（以 camera.forward 为基准）
        var cam = _player.PlayerCameraService?.PlayerCamera;
        if (cam != null)
        {
            var forward = cam.transform.forward;
            Gizmos.color = Color.cyan;
            var left = Quaternion.AngleAxis(-_frontFovHalfAngle, Vector3.up) * forward;
            var right = Quaternion.AngleAxis(_frontFovHalfAngle, Vector3.up) * forward;
            Gizmos.DrawLine(center, center + left * _lockRadius);
            Gizmos.DrawLine(center, center + right * _lockRadius);
        }
        // 当前目标连线
        if (_currentTarget != null && _currentTarget.LockCenter != null)
        {
            Gizmos.color = IsTargetInFront(_currentTarget) && IsTargetValid(_currentTarget) ? Color.green : Color.red;
            Gizmos.DrawLine(center, _currentTarget.LockCenter.position);
        }
    }
    #endregion
}

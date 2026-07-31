using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 通用缩放反馈：鼠标悬停或被选中（键盘 / 手柄导航）时放大，离开时平滑还原。
/// 必须挂在 Selectable 根物体（Button / Slider 本身）上，才能收到 OnSelect 事件；
/// 想只缩放某个子物体（如 Slider 的 Handle 图片），把 _targetToScale 指向它即可，无需挂在子物体上。
/// 基础缩放取 Awake 时缩放目标的 localScale。
/// </summary>
public class ButtonScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float _scaleOnHover = 1.2f;
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private Ease _ease = Ease.OutBack;
    [SerializeField] private Transform _targetToScale; // 留空=缩放自身；挂在 Selectable 根上时指定子物体（如 Handle）

    private Vector3 _baseScale;
    private bool _hovered;
    private bool _selected;
    private Tween _tween;

    private Transform ScaleTarget => _targetToScale != null ? _targetToScale : transform;

    private void Awake() => _baseScale = ScaleTarget.localScale;

    private void PlayScale(bool enlarge_)
    {
        _tween?.Kill();
        _tween = ScaleTarget.DOScale(enlarge_ ? _baseScale * _scaleOnHover : _baseScale, _duration)
            .SetEase(_ease);
    }

    public void OnPointerEnter(PointerEventData _) => SetHovered(true);
    public void OnPointerExit(PointerEventData _) => SetHovered(false);
    public void OnSelect(BaseEventData _) => SetSelected(true);
    public void OnDeselect(BaseEventData _) => SetSelected(false);

    private void SetHovered(bool value_)
    {
        _hovered = value_;
        PlayScale(_hovered || _selected);
    }

    private void SetSelected(bool value_)
    {
        _selected = value_;
        PlayScale(_hovered || _selected);
    }

    private void OnDestroy() => _tween?.Kill();
}

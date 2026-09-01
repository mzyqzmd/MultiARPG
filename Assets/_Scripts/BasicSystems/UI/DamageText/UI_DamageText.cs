using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 世界空间伤害飘字（一次性表现，自包含无 Presenter）：生成于命中点并始终面向本机相机（Billboard），
/// 播放「弹出缩放 → 上浮 → 淡出」后自动回池；表现参数全部序列化可调，回池前清空补间与残留状态。
/// 继承 UI_BaseView 仅为进 SO_UIDatabase 预制体字典；override LateUpdate 刻意不调 base，
/// 以跳过基类的 EventSystem 选中兜底（飘字无 Selectable，该逻辑恒空转）。
/// </summary>
public class UI_DamageText : UI_BaseView
{
    private const float PopDuration = 0.18f;
    private const float FadeDuration = 0.4f;

    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Color _color = new Color(1f, 0.45f, 0.15f);
    [Tooltip("弹出动画的目标缩放倍数（以预制体自身缩放为基准）")]
    [SerializeField] private float _targetScale = 1f;
    [Tooltip("上浮高度（世界单位）")]
    [SerializeField] private float _riseHeight = 0.8f;
    [Tooltip("整个飘字的存活时长（秒）")]
    [SerializeField] private float _duration = 1f;
    [Tooltip("生成点在相机屏幕平面内的随机偏移半径（世界单位，防连续命中数字重叠）")]
    [SerializeField] private float _randomOffsetRadius = 0.25f;

    private DamageTextService _service;
    private Camera _camera;
    private Sequence _sequence;
    private Vector3 _baseScale;

    // 记录预制体自身缩放：世界空间 Canvas 的 scale 通常远小于 1（如 0.01），弹出动画须以它为基准缩放。
    private void Awake() => _baseScale = transform.localScale;

    private void OnDestroy() => _sequence?.Kill();

    /// <summary>绑定所属服务，用于动画结束时回池。</summary>
    public void BindService(DamageTextService service_) => _service = service_;

    #region 在指定世界坐标弹出伤害数字
    /// <summary>在 worldPosition_（带随机屏幕平面偏移）生成并播放「弹出 → 上浮 → 淡出」，播完自动回池。</summary>
    public void Initialize(int damage_, Vector3 worldPosition_)
    {
        _sequence?.Kill();
        _text.text = damage_.ToString();
        _text.color = _color;
        _text.alpha = 1f;

        CacheCamera();
        transform.position = worldPosition_ + RandomScreenOffset();
        transform.localScale = Vector3.zero;

        _sequence = DOTween.Sequence();
        _sequence.Append(transform.DOScale(_baseScale * _targetScale, PopDuration).SetEase(Ease.OutBack));
        _sequence.Join(transform.DOMoveY(transform.position.y + _riseHeight, _duration).SetEase(Ease.OutCubic));
        _sequence.Insert(Mathf.Max(0f, _duration - FadeDuration), _text.DOFade(0f, FadeDuration));
        _sequence.OnComplete(Recycle);
    }
    #endregion

    #region 入池前清空补间与残留状态
    /// <summary>确保复用实例无动画叠加（由对象池的 onRelease 回调调用）。</summary>
    public void ClearForReuse()
    {
        _sequence?.Kill();
        _sequence = null;
        _text.alpha = 1f;
    }
    #endregion

    // Billboard：始终与本机相机同朝向。刻意不调 base.LateUpdate()，跳过基类 EventSystem 选中兜底。
    protected override void LateUpdate()
    {
        CacheCamera();
        if (_camera == null) return;
        transform.rotation = _camera.transform.rotation;
    }

    private void Recycle() => _service.Release(this);

    private void CacheCamera()
    {
        if (_camera == null) _camera = Camera.main;
    }

    /// <summary>在相机屏幕平面内取随机圆盘偏移，保证任意视角下偏移都是视觉上的水平/垂直错位。</summary>
    private Vector3 RandomScreenOffset()
    {
        if (_camera == null || _randomOffsetRadius <= 0f) return Vector3.zero;
        Vector2 disk = Random.insideUnitCircle * _randomOffsetRadius;
        return _camera.transform.right * disk.x + _camera.transform.up * disk.y;
    }
}

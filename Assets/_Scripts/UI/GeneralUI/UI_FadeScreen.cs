using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 全屏黑幕遮罩（常驻 UIRoot 下、独立高排序 Canvas）：提供渐黑/渐入，
/// 供场景切换时在 "黑幕完成" 后再切场景、切换完成后淡入。
/// </summary>
public class UI_FadeScreen : UI_BaseView
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeTime = 0.8f;
    private Tween _tween;

    // 初始必须透明且不拦截射线，否则常驻全屏遮罩会盖住主菜单并吞掉所有点击。
    private void Awake()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }

    #region 渐黑 / 渐入
    /// <summary>alpha 0→1，完成时回调 onComplete_（场景切换在此回调内发起）。</summary>
    public void FadeToBlack(Action onComplete_)
    {
        _tween?.Kill();
        _canvasGroup.blocksRaycasts = true;
        _tween = _canvasGroup.DOFade(1f, _fadeTime).OnComplete(() => onComplete_?.Invoke());
    }

    /// <summary>alpha 1→0，完成时放开射线拦截。</summary>
    public void FadeFromBlack()
    {
        _tween?.Kill();
        _tween = _canvasGroup.DOFade(0f, _fadeTime).OnComplete(() => _canvasGroup.blocksRaycasts = false);
    }
    #endregion
}

using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 顶部弹出提示文本：放大弹出 + 短暂停留 + 渐隐后自销毁。
/// 由 UIService.SpawnUiView 实例化后调 Initialize 设文本与动画参数，动画在 Initialize 末尾播放（Show 仅负责激活，先于 Initialize 触发）。
/// </summary>
public class UI_PopText : UI_BaseView
{
    [SerializeField] private TextMeshProUGUI _text;

    public void Initialize(string message_, Color color_, float targetScale_ = 1f, bool needDestroy_ = true)
    {
        _text.text = message_;
        _text.color = color_;
        PlayPop(targetScale_, needDestroy_);
    }

    #region 弹出并渐隐后自销毁
    private void PlayPop(float targetScale_, bool _needDestroy_)
    {
        transform.DOKill();
        _text.DOKill();
        transform.localScale = Vector3.zero;
        _text.alpha = 1f;
        Sequence pop = DOTween.Sequence();
        pop.Append(transform.DOScale(targetScale_, 0.2f).SetEase(Ease.OutBack));
        if (_needDestroy_)
            pop.AppendInterval(0.5f);
        else
            pop.AppendInterval(60f);
        pop.Append(_text.DOFade(0f, 0.4f));
        pop.OnComplete(() => Destroy(gameObject));
    }
    #endregion

    private void OnDestroy() => DOTween.Kill(gameObject);
}

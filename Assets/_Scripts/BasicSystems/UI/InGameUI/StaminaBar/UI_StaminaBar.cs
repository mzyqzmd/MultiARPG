using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 耐力条子视图（MVP 的 View）：纯展示，仅暴露 SetFillRatio，不持有数据、不自行刷新。
/// 作为 UI_InGameUI 的子视图，其刷新由 InGameUIPresenter 统一驱动（详情见 InGameUIPresenter）。
/// </summary>
public class UI_StaminaBar : UI_BaseView
{
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _fillImage;
    [SerializeField] private Gradient _gradient;

    public void SetFillRatio(float ratio_)
    {
        _slider.value = ratio_;
        _fillImage.color = _gradient.Evaluate(ratio_);
    }
}

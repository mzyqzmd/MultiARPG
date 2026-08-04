using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_HealthBar : UI_BaseView
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

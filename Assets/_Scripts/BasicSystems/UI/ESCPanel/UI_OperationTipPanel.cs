using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 操作说明面板（View）：极简面板，仅一个关闭按钮，打开时由自身绑定点击关闭。
/// </summary>
public class UI_OperationTipPanel : UI_BaseView
{
    [SerializeField] private Button _closeButton;

    public event Action OnClosed;

    #region 初始化（绑定关闭 + 自动聚焦关闭按钮）
    public void Initialize()
    {
        _closeButton.onClick.AddListener(Close);
        _closeButton.Select();
    }
    #endregion

    // 关闭并销毁自身
    private void Close()
    {
        OnClosed?.Invoke();
        Destroy(gameObject);
    }
}

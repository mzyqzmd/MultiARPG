using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 所有 UI 视图的基类（MonoBehaviour）：作为 UI 视图的统一基类/标记，自身不持有任何数据。
/// 数据由各自的 Presenter 以构造函数注入持有，视图只暴露展示方法，由 Presenter 每帧驱动。
/// SO_UIDatabase 以此类型集中管理各面板预制体引用。
/// </summary>
public abstract class UI_BaseView : MonoBehaviour
{
    // 本视图层级内最近一次有效的选中控件；鼠标点空白清空选中后用于兜底恢复焦点。
    private Selectable _lastSelected;

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    // 选中兜底：EventSystem 当前选中对象因点空白变 null 时，恢复到本视图层级内最近有效的控件，
    // 保证键盘/手柄导航与确认键持续可用。LateUpdate 晚于各子类 Update 执行；非激活物体不跑。
    protected virtual void LateUpdate()
    {
        if (EventSystem.current == null) return;
        var current = EventSystem.current.currentSelectedGameObject;
        if (current != null)
        {
            if (current.transform.IsChildOf(transform))
                _lastSelected = current.GetComponent<Selectable>();
        }
        else if (_lastSelected != null && _lastSelected.isActiveAndEnabled)
        {
            _lastSelected.Select();
        }
    }
}

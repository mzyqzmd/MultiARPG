using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统一 UI 服务（IService）：运行时在组合根下构建持久 UIRoot（跨场景不销毁），
/// 从 SO_UIDatabase 克隆 BasicCanvas 生成 Static/Hud/Overlay 三层画布（按更新频率分层，规避单 Canvas 全量重建），
/// 持有本地玩家引用并提供绑定事件；Spawn 按 key 从数据库取面板预制体、实例化到指定层，避免代码硬编码构建 UI。
/// 通用 UI 框架，不耦合任何具体面板。
/// </summary>
public class UIService : IService
{
    private readonly Transform _uiRoot;
    private readonly SO_UIDatabase _database;
    private readonly Dictionary<EUILayerCanvas, Transform> _layers = new(); // 分层

    private readonly UI_FadeScreen _fadeScreen; // 常驻全屏遮罩（非分层，独立 Canvas sortingOrder 100）
    private UI_EscPanel _escPanel; // ESC 暂停面板缓存引用（防重复打开）

    public Player LocalPlayer { get; private set; }

    public UIService(Transform gameRoot_, SO_UIDatabase uiDatabase_)
    {
        var root = new GameObject("UIRoot");
        root.transform.SetParent(gameRoot_);
        _uiRoot = root.transform;

        _database = uiDatabase_;

        // 按分层克隆 BasicCanvas，SortingOrder 决定绘制顺序（Static 最底 / Overlay 最顶）
        CreateLayerCanvas(EUILayerCanvas.Static, "StaticCanvas", 0);
        CreateLayerCanvas(EUILayerCanvas.Hud, "HudCanvas", 10);
        CreateLayerCanvas(EUILayerCanvas.Overlay, "OverlayCanvas", 20);

        _fadeScreen = UnityEngine.Object.Instantiate(_database.GetUIPrefab("FadeScreen") as UI_FadeScreen, _uiRoot, false);
    }

    #region 绑定本地玩家
    public void BindLocalPlayer(Player player_)
    {
        LocalPlayer = player_;
        SpawnUiView<UI_InGameUI>("InGameUI", EUILayerCanvas.Hud).Initialize(player_);
    }

    public void UnbindLocalPlayer()
    {
        LocalPlayer = null;
        ClearAllLayerView();
    }
    #endregion

    #region 按 key 从数据库取面板预制体，实例化到指定层，返回其上的 T 组件。
    public T SpawnUiView<T>(string key_, EUILayerCanvas layer_) where T : Component
    {
        UI_BaseView newView = UnityEngine.Object.Instantiate(
            _database.GetUIPrefab(key_), _layers[layer_], false);
        newView.gameObject.SetActive(false);
        newView.Show();
        return newView.GetComponent<T>();
    }
    #endregion

    #region 按 key 取面板预制体（供需自行指定父物体者，如列表子项）
    public UI_BaseView GetUiPrefab(string key_) => _database.GetUIPrefab(key_);
    #endregion

    #region ESC 暂停面板管理（世界场景内由 GameRoot 订阅输入事件调用）
    /// <summary>弹出 ESC 暂停面板并暂停玩法输入（面板自身在 Initialize 内处理输入暂停）。防重复打开。</summary>
    public void OpenEscPanel()
    {
        if (_escPanel != null) return;
        _escPanel = SpawnUiView<UI_EscPanel>("EscPanel", EUILayerCanvas.Overlay);
        _escPanel.Initialize(this);
    }

    /// <summary>关闭 ESC 暂停面板并销毁（输入恢复由面板按钮显式处理）。</summary>
    public void CloseEscPanel()
    {
        if (_escPanel == null) return;
        UnityEngine.Object.Destroy(_escPanel.gameObject);
        _escPanel = null;
    }

    /// <summary>ESC 面板当前是否处于打开状态。</summary>
    public bool IsEscPanelOpen => _escPanel != null;
    #endregion


    #region 创建Canvas / 清空Canvas下的面板
    /// <summary>创建指定层。</summary>
    private void CreateLayerCanvas(EUILayerCanvas layer_, string name_, int sortingOrder_)
    {
        Canvas canvasGo = UnityEngine.Object.Instantiate(_database.BasicCanvasPrefab, _uiRoot, false);
        canvasGo.name = name_;
        canvasGo.sortingOrder = sortingOrder_;
        _layers[layer_] = canvasGo.transform;
    }
    /// <summary>清空指定层下的所有已生成面板。</summary>
    public void ClearLayerView(EUILayerCanvas layer_)
    {
        var layer = _layers[layer_];
        for (int i = layer.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(layer.GetChild(i).gameObject);
    }

    /// <summary>清空所有层的已生成面板（本地玩家解绑时调用）。</summary>
    public void ClearAllLayerView()
    {
        foreach (var layer in _layers.Values)
            for (int i = layer.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(layer.GetChild(i).gameObject);
    }
    #endregion

    #region 场景切换黑幕（常驻 FadeScreen，null 守卫：未注册则不遮挡）
    public void FadeToBlack(Action onComplete_) =>
        _fadeScreen.FadeToBlack(onComplete_);

    public void FadeFromBlack() => _fadeScreen.FadeFromBlack();
    #endregion
}
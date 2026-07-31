using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 存档槽面板：展示全部存档槽并提供 新建/返回 按钮，选中或新建后升 Host 并加载世界。
/// 原 MVP 的 Presenter 已合并进本类（逻辑量小，无需额外 Presenter 层）。
/// </summary>
public class UI_SaveSlotPanel : UI_BaseView
{
    [SerializeField] private Button _createNewSave;
    [SerializeField] private Button _returnButton;
    [SerializeField] private ScrollRect _scrollRect;

    private UIService _uiService;
    private GameSaveService _saveService;
    private NetworkService _networkService;
    private SceneControlService _sceneControlService;
    private Action _onClose;
    private GameObject _lastSelectedSlot;

    #region 初始化并刷新槽列表
    public void Initialize(UIService uiService_, GameSaveService saveService_, NetworkService networkService_,
        SceneControlService sceneControlService_, Action onClose_)
    {
        _uiService = uiService_;
        _saveService = saveService_;
        _networkService = networkService_;
        _sceneControlService = sceneControlService_;
        _onClose = onClose_;

        _createNewSave.onClick.RemoveListener(CreateNewSlot);
        _createNewSave.onClick.AddListener(CreateNewSlot);
        _returnButton.onClick.RemoveListener(Return);
        _returnButton.onClick.AddListener(Return);
        Refresh();

        _createNewSave.Select();
    }
    #endregion

    #region 按钮回调
    // 流程：新建存档
    private void CreateNewSlot()
    {
        string popText;
        Color color;
        if (_saveService.CreateNewSlot())
        {
            Refresh();
            popText = "已创建存档";
            color = Color.green;
        }
        else
        {
            popText = "存档上限为10，存档创建失败";
            color = Color.red;
        }
        _uiService.SpawnUiView<UI_PopText>("PopText", EUILayerCanvas.Overlay).
            Initialize(popText, color);
    }
    // 返回主菜单（触发还原回调并隐藏自身）
    public void Return()
    {
        _onClose?.Invoke();
        Hide();
    }
    #endregion

    #region 对外 API：由槽子项回调
    // 选择已有槽并开始游戏
    public void SelectSlot(string slotId_)
    {
        _saveService.SelectSlot(slotId_);
        _networkService.StartHost();
        _sceneControlService.LoadWorldScene();
    }

    // 删除槽并刷新列表
    public void DeleteSlot(string slotId_)
    {
        _saveService.DeleteSlot(slotId_);
        Refresh();
        _uiService.SpawnUiView<UI_PopText>("PopText", EUILayerCanvas.Overlay).
           Initialize("已删除存档", Color.gray);
        _createNewSave.Select();
    }
    #endregion

    #region 自动滚动到选中槽
    // 手柄/键盘导航选中列表内某槽时，将其滚入视口中心
    private void Update()
    {
        if (EventSystem.current == null) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == _lastSelectedSlot) return;
        _lastSelectedSlot = selected;
        if (selected == null || !selected.transform.IsChildOf(_scrollRect.content)) return;
        Transform slot = selected.transform;
        while (slot.parent != _scrollRect.content) slot = slot.parent;
        ScrollToSlot(slot as RectTransform);
    }

    // 计算目标槽与当前视口中心的偏移，调整垂直归一化位置把它带入可视区
    private void ScrollToSlot(RectTransform target_)
    {
        Canvas.ForceUpdateCanvases();
        RectTransform content = _scrollRect.content;
        RectTransform viewport = _scrollRect.viewport;
        if (content == null || viewport == null) return;
        // 目标中心 / 视口中心都转换到 content 本地坐标系，求 Y 方向像素差
        Vector2 targetCenter = content.InverseTransformPoint(target_.TransformPoint(target_.rect.center));
        Vector2 viewportCenter = content.InverseTransformPoint(viewport.TransformPoint(viewport.rect.center));
        float scrollable = content.rect.height - viewport.rect.height;
        if (scrollable <= 0f) return;
        float delta = (targetCenter.y - viewportCenter.y) / scrollable;
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + delta);
    }
    #endregion

    #region 刷新槽列表
    private void Refresh()
    {
        for (int i = _scrollRect.content.childCount - 1; i >= 0; i--)
            Destroy(_scrollRect.content.GetChild(i).gameObject);
        foreach (var (slotId, meta) in _saveService.ListSlots())
        {
            var prefab = _uiService.GetUiPrefab("SaveSlot") as UI_SaveSlot;
            Instantiate(prefab, _scrollRect.content).Initialize(slotId, meta, this);
        }
    }
    #endregion
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个存档槽视图（MVP 的 View）：展示角色名 / 游玩时长，提供选择 / 删除按钮。
/// </summary>
public class UI_SaveSlot : UI_BaseView
{
    [SerializeField] private Image _playerPortraitImage;
    [SerializeField] private TextMeshProUGUI _playerNameText;
    [SerializeField] private TextMeshProUGUI _timePlayedText;
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _deleteButton;

    private string _slotId;
    private UI_SaveSlotPanel _panel;

    // 绑定槽数据并接好选择/删除回调（回调指向面板本身）
    // 单文件多玩家：展示首位玩家（Host）的角色名/时长，空槽显示占位
    public void Initialize(string slotId_, GameSaveData data_, UI_SaveSlotPanel panel_)
    {
        _slotId = slotId_;
        bool hasPlayer = data_.Players != null && data_.Players.Count > 0;
        _playerNameText.text = hasPlayer ? data_.Players[0].characterName : "空存档";
        _timePlayedText.text = FormatTime(data_.World?.secondsPlayed ?? 0f);
        _panel = panel_;

        _selectButton.onClick.AddListener(Select);
        _deleteButton.onClick.AddListener(Delete);
    }

    // 选择该槽（由面板升 Host 并加载世界）
    public void Select() => _panel.SelectSlot(_slotId);

    // 删除该槽（由面板刷新列表）
    public void Delete() => _panel.DeleteSlot(_slotId);

    #region 秒数格式化
    private string FormatTime(float seconds_)
    {
        int total = Mathf.FloorToInt(seconds_);
        int h = total / 3600;
        int m = (total % 3600) / 60;
        int s = total % 60;
        return $"{h:D2}:{m:D2}:{s:D2}";
    } 
    #endregion
}

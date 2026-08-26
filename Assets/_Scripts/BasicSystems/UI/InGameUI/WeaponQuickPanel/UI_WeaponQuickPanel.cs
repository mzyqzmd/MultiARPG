using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 武器快捷面板（MVP 的 View）：实时反映各装备槽位状态。
/// 装备时显示武器图标（正常色）；收起（仍持有但未装备）显示同图标但置灰；槽位无武器时显示 _emptyIcon。
/// 逻辑量小，直接订阅 PlayerEquipmentControl 事件，不另设 Presenter 层。
/// </summary>
public class UI_WeaponQuickPanel : UI_BaseView
{
    [Header("Slot为空时的Icon")]
    [SerializeField] private Sprite _emptyIcon;
    [Header("武器Slot")]
    [SerializeField] private Image _leftHandWeaponImage;
    [SerializeField] private Image _backWeaponImage;
    [SerializeField] private Image _rightHandWeaponImage;
    [Header("道具Slot")]
    [SerializeField] private Image _leftHipPropImage;
    [SerializeField] private Image _rightHipPropImage;

    /// <summary>每个槽位最后装备过的物品实例缓存，供收起时取图标置灰（实例已 IsEquipped=false 但仍在 _slotItems）。</summary>
    private readonly Dictionary<EEquipmentSlot, IEquippable> _lastEquipped = new();
    /// <summary>槽位枚举到对应 Image 的映射，避免重复分支。</summary>
    private readonly Dictionary<EEquipmentSlot, Image> _slotImages = new();
    /// <summary>订阅来源，OnDestroy 反订阅配对。</summary>
    private PlayerEquipmentControl _equipmentControl;

    #region 初始化与订阅
    /// <summary>注入装备控制并订阅装备/卸下事件；建立 slot→Image 映射，初始全部回退空图标。</summary>
    public void Initialize(PlayerEquipmentControl equipmentControl_)
    {
        _equipmentControl = equipmentControl_;
        _slotImages[EEquipmentSlot.LeftHand] = _leftHandWeaponImage;
        _slotImages[EEquipmentSlot.Back] = _backWeaponImage;
        _slotImages[EEquipmentSlot.RightHand] = _rightHandWeaponImage;
        _slotImages[EEquipmentSlot.LeftHip] = _leftHipPropImage;
        _slotImages[EEquipmentSlot.RightHip] = _rightHipPropImage;
        foreach (var slot in _slotImages.Keys)
            SetSlotEmpty(slot);
        _equipmentControl.OnEquipped += OnEquipped;
        _equipmentControl.OnUnequipped += OnUnequipped;
    }

    /// <summary>组件销毁时反订阅，与 Initialize 配对防止重 spawn 事件累积。</summary>
    private void OnDestroy()
    {
        if (_equipmentControl != null)
        {
            _equipmentControl.OnEquipped -= OnEquipped;
            _equipmentControl.OnUnequipped -= OnUnequipped;
        }
    }
    #endregion

    #region 槽位刷新
    /// <summary>装备回调：记录缓存并以正常色显示物品图标。</summary>
    private void OnEquipped(EEquipmentSlot slot_, IEquippable item_)
    {
        _lastEquipped[slot_] = item_;
        RefreshSlot(slot_, item_, true);
    }

    /// <summary>卸下回调：读缓存置灰同图标；若缓存缺失（彻底无物品）回退空图标。</summary>
    private void OnUnequipped(EEquipmentSlot slot_)
    {
        if (_lastEquipped.TryGetValue(slot_, out var item) && item != null)
            RefreshSlot(slot_, item, false);
        else
            SetSlotEmpty(slot_);
    }

    /// <summary>按装备状态刷新槽位：装备显示彩色图标，收起显示灰色同图标。</summary>
    private void RefreshSlot(EEquipmentSlot slot_, IEquippable item_, bool equipped_)
    {
        var image = GetSlotImage(slot_);
        if (image == null) return;
        image.sprite = item_. ItemSprite ? item_.ItemSprite : _emptyIcon;
        image.color = equipped_ ? Color.white : Color.gray;
    }

    /// <summary>槽位彻底无武器：显示空图标且正常色。</summary>
    private void SetSlotEmpty(EEquipmentSlot slot_)
    {
        var image = GetSlotImage(slot_);
        if (image == null) return;
        image.sprite = _emptyIcon;
        image.color = Color.white;
    }

    /// <summary>按枚举取对应槽位 Image（未配置返回 null）。</summary>
    private Image GetSlotImage(EEquipmentSlot slot_) =>
        _slotImages.TryGetValue(slot_, out var image) ? image : null;
    #endregion
}

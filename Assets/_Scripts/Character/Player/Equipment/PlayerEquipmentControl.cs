using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>装备控制（MonoBehaviour，挂 Player.prefab）：本地挂载权威，管理五个挂载点，统一占用表 _slotTaken 记录槽位是否被占。
/// 装备/卸下目标槽位直接读 SO_BaseItem 上的 EquipTargetSlots/UnequipTargetSlots（不缓存运行时副本）。
/// Toggle 仅做槽位状态决策（即时翻转 _slotTaken），Equip/Unequip 在动画帧被调用时执行真实 Transform 挂载并触发事件。
/// 装备切换编排（热键订阅、动画帧提交、拔刀/收刀动画、IsBusy、重击 tier 桥接）由本组件经 [SerializeField] 自订阅，不依赖 Player 桥接。</summary>
public class PlayerEquipmentControl : MonoBehaviour
{
    [Header("玩家引用")]
    [SerializeField] private Player _player;
    [Header("默认武器(测试用的)")]
    [SerializeField] private BaseItem _defaultWeapon;
    [Header("装备挂载点")]
    [SerializeField] private Transform _leftHand;
    [SerializeField] private Transform _rightHand;
    [SerializeField] private Transform _leftHip;
    [SerializeField] private Transform _rightHip;
    [SerializeField] private Transform _back;

    /// <summary>当前切换流程中待提交挂载的物品。</summary>
    public BaseItem PendingItem { get; set; }
    /// <summary>当前切换流程的目标槽位。</summary>
    public EEquipmentSlot PendingSlot { get; private set; }

    /// <summary>槽位占用表：真实槽位是否被占用（IsOccupying==false 的表现挂不写入，可无限叠加）。</summary>
    private readonly Dictionary<EEquipmentSlot, bool> _slotTaken = new();
    /// <summary>槽位实例缓存：每个真实占用槽当前挂接的物品实例列表（含表现挂），供 GetEquippedItem 查询。</summary>
    private readonly Dictionary<EEquipmentSlot, List<BaseItem>> _slotItems = new();

    /// <summary>物品挂上某挂载点时触发（参数：主槽 + 被挂接实例），供外部音效/UI 订阅。</summary>
    public event Action<EEquipmentSlot, BaseItem> OnEquipped;

    /// <summary>物品从某挂载点卸下时触发（参数为原主槽），供外部音效/UI 订阅。</summary>
    public event Action<EEquipmentSlot> OnUnequipped;

    /// <summary>组件启用时自订阅输入与动画事件；重置全部槽位占用状态为"空闲"。</summary>
    private void Awake()
    {
        foreach (EEquipmentSlot slot in Enum.GetValues(typeof(EEquipmentSlot)))
            _slotTaken[slot] = false;
        _player.InputService.OnHotbarTriggered += OnHotbarTriggered;
    }

    /// <summary>组件销毁时反订阅，与 Awake 配对防止重 spawn 事件累积。</summary>
    private void OnDestroy()
    {
        _player.InputService.OnHotbarTriggered -= OnHotbarTriggered;
    }
    #region 装备切换编排（自订阅）
    /// <summary>热键栏回调：本地玩家切换指定槽位装备（拔刀/收刀表现）。仅做状态决策（翻转占用表）并播动画，真实挂载在 OnToggleFrame 帧事件提交。</summary>
    private void OnHotbarTriggered(EEquipmentSlot slot_)
    {
        BaseItem item = _defaultWeapon;//NOTE:临时测试用
        if (item == null) return; // 未提供则不切换
        var mainSlot = item.ItemData.EquipTargetSlots[0].Slot;
        if (slot_ != mainSlot) return; // 热键槽位与武器主装备槽不符则不响应
        Toggle(slot_);
        PendingItem = item;
        PendingSlot = slot_;
        _player.StateControl.IsBusy = true;
        _player.Anim.SetTrigger("Equip1HandWeaponTrigger");
    }
    #endregion

    #region 切换与挂载
    /// <summary>切换指定槽位装备状态：即时翻转 _slotTaken 占用表（避免动画途中重复触发误判）。
    /// 真实 Transform 挂载由动画帧调 Equip/Unequip 提交，本方法不直接挂物体。</summary>
    public void Toggle(EEquipmentSlot slot_) => _slotTaken[slot_] = !_slotTaken[slot_];

    /// <summary>动画帧提交：物品按 EquipTargetSlots 挂载到各槽位（IsOccupying==true 才登记占用，false 仅表现挂），
    /// 触发 OnEquipped（主槽取数组首个槽位）。</summary>
    public void Equip(BaseItem item_)
    {
        var slots = item_.ItemData.EquipTargetSlots;
        var mainSlot = slots.Length > 0 ? slots[0].Slot : EEquipmentSlot.RightHand;
        foreach (var e in slots)
        {
            if (e.IsOccupying) _slotTaken[e.Slot] = true;
            RegisterToSlot(e.Slot, item_);
            var t = GetSlotTransform(e.Slot);
            if (t != null)
            {
                item_.transform.SetParent(t);
                item_.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
        item_.IsEquipped = true;
        item_.RegisterOwner(_player);
        OnEquipped?.Invoke(mainSlot, item_);
    }

    /// <summary>动画帧提交：按 UnequipTargetSlots 把物品挂到目标槽位（含表现挂），
    /// 清理 EquipTargetSlots 中真正占用项，触发 OnUnequipped。</summary>
    public void Unequip(BaseItem item_)
    {
        if (item_ == null) return;
        var equipSlots = item_.ItemData.EquipTargetSlots;
        foreach (var e in equipSlots)
            if (e.IsOccupying)
                _slotTaken[e.Slot] = false;

        var unequipSlots = item_.ItemData.UnequipTargetSlots;
        foreach (var e in unequipSlots)
        {
            var t = GetSlotTransform(e.Slot);
            if (t != null)
            {
                item_.transform.SetParent(t);
                item_.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
        item_.IsEquipped = false;
        UnregisterFromSlot(equipSlots, item_);
        var mainSlot = equipSlots.Length > 0 ? equipSlots[0].Slot : EEquipmentSlot.RightHand;
        OnUnequipped?.Invoke(mainSlot);
    }

    /// <summary>卸下并销毁全部槽位物品。</summary>
    public void UnequipAll()
    {
        foreach (EEquipmentSlot slot in Enum.GetValues(typeof(EEquipmentSlot)))
            if (_slotTaken.TryGetValue(slot, out var taken) && taken)
                _slotTaken[slot] = false;
        _slotItems.Clear();
        foreach (var item in _back.GetComponentsInChildren<BaseItem>())
            Destroy(item.gameObject);
        foreach (var item in _leftHand.GetComponentsInChildren<BaseItem>())
            Destroy(item.gameObject);
        foreach (var item in _rightHand.GetComponentsInChildren<BaseItem>())
            Destroy(item.gameObject);
        foreach (var item in _leftHip.GetComponentsInChildren<BaseItem>())
            Destroy(item.gameObject);
        foreach (var item in _rightHip.GetComponentsInChildren<BaseItem>())
            Destroy(item.gameObject);
    }
    #endregion

    #region 查询
    /// <summary>指定槽位当前是否被占用。</summary>
    public bool IsSlotTaken(EEquipmentSlot slot_) =>
        _slotTaken.TryGetValue(slot_, out var taken) && taken;

    /// <summary>取指定槽位当前已装备（IsEquipped==true）的物品实例；无则返回 null。</summary>
    public BaseItem GetEquippedItem(EEquipmentSlot slot_)
    {
        if (_slotItems.TryGetValue(slot_, out var list))
            foreach (var item in list)
                if (item.IsEquipped) return item;
        return null;
    }

    /// <summary>按枚举取对应挂载点 Transform（未配置返回 null）。</summary>
    private Transform GetSlotTransform(EEquipmentSlot slot_) => slot_ switch
    {
        EEquipmentSlot.LeftHand => _leftHand,
        EEquipmentSlot.RightHand => _rightHand,
        EEquipmentSlot.LeftHip => _leftHip,
        EEquipmentSlot.RightHip => _rightHip,
        EEquipmentSlot.Back => _back,
        _ => null
    };

    /// <summary>把物品实例登记到指定槽位的缓存列表（列表按需创建）。</summary>
    private void RegisterToSlot(EEquipmentSlot slot_, BaseItem item_)
    {
        if (!_slotItems.TryGetValue(slot_, out var list))
            _slotItems[slot_] = list = new List<BaseItem>();
        if (!list.Contains(item_)) list.Add(item_);
    }

    /// <summary>从各给定槽位的缓存列表移除物品实例（卸下/销毁时清理）。</summary>
    private void UnregisterFromSlot(EquipSlot[] slots_, BaseItem item_)
    {
        foreach (var e in slots_)
            if (_slotItems.TryGetValue(e.Slot, out var list))
                list.Remove(item_);
    }
    #endregion
}

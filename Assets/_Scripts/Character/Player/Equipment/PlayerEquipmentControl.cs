using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

/// <summary>装备控制（MonoBehaviour，挂 Player.prefab）：本地挂载权威，管理五个挂载点，统一占用表 _slotTaken 记录槽位是否被占。
/// 装备/卸下目标槽位直接读 IEquippable 上的 EquipTargetSlots/UnequipTargetSlots（不缓存运行时副本）。
/// 切换意图显式记录于 _pendingAction（不再把 _slotTaken 当信号位），Equip/Unequip 在动画帧被调用时执行真实 Transform 挂载并触发事件。
/// 装备切换编排（热键订阅、动画帧提交、拔刀/收刀动画、IsBusy、重击 tier 桥接）由本组件经 [SerializeField] 自订阅，不依赖 Player 桥接。</summary>
public class PlayerEquipmentControl : MonoBehaviour
{
    [Header("玩家引用")]
    [SerializeField] private Player _player;
    [SerializeField] private BaseWeapon _defaultWeapon;
    [Header("装备挂载点")]
    [SerializeField] private Transform _leftHand;
    [SerializeField] private Transform _rightHand;
    [SerializeField] private Transform _leftHip;
    [SerializeField] private Transform _rightHip;
    [SerializeField] private Transform _back;

    /// <summary>当前切换流程中待提交挂载的物品。</summary>
    public IEquippable PendingItem { get; set; }
    /// <summary>当前切换流程的目标槽位。</summary>
    public EEquipmentSlot PendingSlot { get; private set; }
    /// <summary>当前切换流程的显式意图（拔刀/收刀），由动画帧提交时消费，不再依赖 _slotTaken 反推。</summary>
    public EPendingEquipAction PendingAction { get; private set; }
    /// <summary>常驻覆盖控制器：以 AC_Player 为基类包一层壳，运行期引用恒定，仅 ApplyOverrides 改特定 clip 映射。</summary>
    private AnimatorOverrideController _overrideController;

    /// <summary>槽位占用表：真实槽位是否被占用（IsOccupying==false 的表现挂不写入，可无限叠加）。</summary>
    private readonly Dictionary<EEquipmentSlot, bool> _slotTaken = new();
    /// <summary>槽位实例缓存：每个真实占用槽当前挂接的物品实例列表（含表现挂），供 GetEquippedItem 查询。</summary>
    private readonly Dictionary<EEquipmentSlot, List<IEquippable>> _slotItems = new();

    /// <summary>物品挂上某挂载点时触发（参数：主槽 + 被挂接实例），供外部音效/UI 订阅。</summary>
    public event Action<EEquipmentSlot, IEquippable> OnEquipped;

    /// <summary>物品从某挂载点卸下时触发（参数为原主槽），供外部音效/UI 订阅。</summary>
    public event Action<EEquipmentSlot> OnUnequipped;

    /// <summary>组件启用时重置全部槽位占用状态为"空闲"并缓存默认控制器。</summary>
    private void Awake()
    {
        foreach (EEquipmentSlot slot in Enum.GetValues(typeof(EEquipmentSlot)))
            _slotTaken[slot] = false;
        _overrideController = new AnimatorOverrideController(_player.Anim.runtimeAnimatorController);
        _player.Anim.runtimeAnimatorController = _overrideController;
    }
    private void Start()
    {
        Equip(_defaultWeapon);
    }

    #region 输入事件绑定（由 Player.OnNetworkSpawn/OnNetworkDespawn 在 IsOwner 可靠后转发调用）
    /// <summary>订阅本地玩家热键栏输入事件（仅 owner 调用）。</summary>
    public void BindInput() => _player.InputService.OnHotbarTriggered += OnHotbarTriggered;
    /// <summary>反订阅热键栏输入事件（与 BindInput 配对，防重 spawn 累积）。</summary>
    public void UnbindInput() => _player.InputService.OnHotbarTriggered -= OnHotbarTriggered;
    /// <summary>热键栏回调：本地玩家切换指定槽位装备（拔刀/收刀表现）。按槽内是否已装备该物判定意图并记录 _pendingAction，
    /// 真实挂载在 OnToggleFrame 帧事件按意图提交。仅响应目标槽位与物品主装备槽匹配的热键。</summary>
    private void OnHotbarTriggered(EEquipmentSlot slot_)
    {
        IEquippable item = null;
        foreach (var list in _slotItems.Values)
            foreach (var candidate in list)
            {
                bool match = false;
                foreach (var e in candidate.EquipTargetSlots) if (e.Slot == slot_) { match = true; break; }
                if (match) { item = candidate; break; }
            }
        if (item == null) return; // 无对应武器则不切换
        if (slot_ != item.EquipTargetSlots[0].Slot) return; // 热键槽位与武器主装备槽不符则不响应
        PendingAction = IsSlotTaken(slot_) ? EPendingEquipAction.Unequip : EPendingEquipAction.Equip;
        PendingItem = item;
        PendingSlot = slot_;
        _player.Anim.SetTrigger("Equip1HandWeaponTrigger");
    }
    #endregion

    #region 切换与挂载
    /// <summary>动画帧提交（拔刀）：按 EquipTargetSlots 挂载到各槽位并标记已装备，武器额外应用动画 clip 覆盖。</summary>
    public void Equip(IEquippable item_)
    {
        if (item_ == null) return;
        var mainSlot = item_.EquipTargetSlots.Length > 0 ? item_.EquipTargetSlots[0].Slot : EEquipmentSlot.RightHand;
        MountInternal(item_, item_.EquipTargetSlots, true);
        item_.IsEquipped = true;
        item_.RegisterOwner(_player);
        if (item_ is BaseWeapon weapon && weapon.WeaponData.OverrideController != null)
        {
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            weapon.WeaponData.OverrideController.GetOverrides(overrides);
            _overrideController.ApplyOverrides(overrides);
        }
        OnEquipped?.Invoke(mainSlot, item_);
    }

    /// <summary>动画帧提交（收刀）：按 UnequipTargetSlots 把物品挂到收起槽位，清理 EquipTargetSlots 占用项并还原动画 clip 映射。</summary>
    public void Unequip(IEquippable item_)
    {
        if (item_ == null) return;
        var go = item_ as MonoBehaviour;
        var equipSlots = item_.EquipTargetSlots;
        foreach (var e in equipSlots)
            if (e.IsOccupying)
                _slotTaken[e.Slot] = false;

        var unequipSlots = item_.UnequipTargetSlots;
        MountInternal(item_, unequipSlots, false);
        item_.IsEquipped = false;
        foreach (var e in equipSlots)
            if (_slotItems.TryGetValue(e.Slot, out var list))
                list.Remove(item_);
        _overrideController.ApplyOverrides(new List<KeyValuePair<AnimationClip, AnimationClip>>());
        var mainSlot = equipSlots.Length > 0 ? equipSlots[0].Slot : EEquipmentSlot.RightHand;
        OnUnequipped?.Invoke(mainSlot);
    }

    /// <summary>共有挂载循环：按给定槽集合把物品 SetParent + 归零局部变换，IsOccupying==true 的槽登记占用表与实例缓存。
    /// isEquip_=true 时为正式装备（登记真实占用）；false 时为收起态表现挂（不登记占用、不顶掉手持武器映射）。</summary>
    private void MountInternal(IEquippable item_, EquipSlot[] slots_, bool isEquip_)
    {
        var go = item_ as MonoBehaviour;
        if (go == null) return;
        foreach (var e in slots_)
        {
            if (isEquip_ && e.IsOccupying) _slotTaken[e.Slot] = true;
            if (!_slotItems.TryGetValue(e.Slot, out var list))
                _slotItems[e.Slot] = list = new List<IEquippable>();
            if (!list.Contains(item_)) list.Add(item_);
            var t = GetSlotTransform(e.Slot);
            if (t != null)
            {
                go.transform.SetParent(t);
                go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
    }

    /// <summary>卸下并销毁全部槽位物品。</summary>
    public void UnequipAll()
    {
        _overrideController.ApplyOverrides(new List<KeyValuePair<AnimationClip, AnimationClip>>());
        foreach (EEquipmentSlot slot in Enum.GetValues(typeof(EEquipmentSlot)))
            if (_slotTaken.TryGetValue(slot, out var taken) && taken)
                _slotTaken[slot] = false;
        _slotItems.Clear();
        foreach (var item in _back.GetComponentsInChildren<MonoBehaviour>())
            if (item is IEquippable) Destroy(item.gameObject);
        foreach (var item in _leftHand.GetComponentsInChildren<MonoBehaviour>())
            if (item is IEquippable) Destroy(item.gameObject);
        foreach (var item in _rightHand.GetComponentsInChildren<MonoBehaviour>())
            if (item is IEquippable) Destroy(item.gameObject);
        foreach (var item in _leftHip.GetComponentsInChildren<MonoBehaviour>())
            if (item is IEquippable) Destroy(item.gameObject);
        foreach (var item in _rightHip.GetComponentsInChildren<MonoBehaviour>())
            if (item is IEquippable) Destroy(item.gameObject);
    }
    #endregion

    #region 查询
    /// <summary>指定槽位当前是否被占用。</summary>
    public bool IsSlotTaken(EEquipmentSlot slot_) =>
        _slotTaken.TryGetValue(slot_, out var taken) && taken;

    /// <summary>取指定槽位当前已装备（IsEquipped==true）的物品实例；无则返回 null。</summary>
    public IEquippable GetEquippedItem(EEquipmentSlot slot_)
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
    #endregion
}

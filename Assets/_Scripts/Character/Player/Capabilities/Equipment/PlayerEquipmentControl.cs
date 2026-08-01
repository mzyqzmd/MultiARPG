using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>装备控制（普通能力类，纯本地）：管理五处挂载点，每槽位最多一件物品、各槽位可同时持有；挂卸时抛事件供未来拔剑/收剑动画按槽位订阅。</summary>
public class PlayerEquipmentControl
{
    private readonly Transform _leftHand;
    private readonly Transform _rightHand;
    private readonly Transform _leftHip;
    private readonly Transform _rightHip;
    private readonly Transform _back;

    private readonly Dictionary<EEquipmentSlot, GameObject> _equipped = new();

    /// <summary>物品挂上某挂载点时触发（参数：槽位 + 被挂接实例），供拔剑等动画按槽位订阅。</summary>
    public event Action<EEquipmentSlot, GameObject> OnEquipped;

    /// <summary>物品从某挂载点卸下时触发（参数为槽位），供收剑等动画订阅。</summary>
    public event Action<EEquipmentSlot> OnUnequipped;

    /// <summary>构造：注入五个挂载点 Transform（来自 Player 预制体 Inspector 配置）。</summary>
    public PlayerEquipmentControl(Transform leftHand_, Transform rightHand_, Transform leftHip_, Transform rightHip_, Transform back_)
    {
        _leftHand = leftHand_;
        _rightHand = rightHand_;
        _leftHip = leftHip_;
        _rightHip = rightHip_;
        _back = back_;
    }

    #region 挂卸物品
    /// <summary>将物品挂到指定挂载点（局部坐标归零）；同槽位已有物品则先替换销毁。</summary>
    public void Equip(GameObject item_, EEquipmentSlot slot_)
    {
        if (item_ == null) return;
        Transform slotTransform = GetSlotTransform(slot_);
        if (slotTransform == null) return;

        Unequip(slot_);

        _equipped[slot_] = item_;
        item_.transform.SetParent(slotTransform);
        item_.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        OnEquipped?.Invoke(slot_, item_);
    }

    /// <summary>卸下并销毁指定槽位的物品，触发 OnUnequipped。</summary>
    public void Unequip(EEquipmentSlot slot_)
    {
        if (!_equipped.TryGetValue(slot_, out var item) || item == null) return;
        _equipped.Remove(slot_);
        UnityEngine.Object.Destroy(item.gameObject);
        OnUnequipped?.Invoke(slot_);
    }

    /// <summary>卸下并销毁全部槽位的物品。</summary>
    public void UnequipAll()
    {
        foreach (var slot in _equipped.Keys)
            Unequip(slot);
    }
    #endregion

    #region 查询
    /// <summary>取指定槽位当前物品（未装备为 null）。</summary>
    public GameObject GetEquipped(EEquipmentSlot slot_) =>
        _equipped.TryGetValue(slot_, out var item) ? item : null;

    /// <summary>指定槽位是否已装备。</summary>
    public bool IsEquipped(EEquipmentSlot slot_) => _equipped.ContainsKey(slot_);

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

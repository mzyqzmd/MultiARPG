using System;

/// <summary>装备挂载描述：一个目标槽位 + 是否真正占用该槽位。
/// IsOccupying=false 表示仅做视觉表现挂载（如收剑挂左髋），不登记占用表，可无限叠加；
/// IsOccupying=true 表示真正占用该槽位（如药瓶挂左髋），同一槽同时至多一个占用项。</summary>
[Serializable]
public struct EquipSlot
{
    public EEquipmentSlot Slot;
    public bool IsOccupying;

    public EquipSlot(EEquipmentSlot slot_, bool isOccupying_)
    {
        Slot = slot_;
        IsOccupying = isOccupying_;
    }
}

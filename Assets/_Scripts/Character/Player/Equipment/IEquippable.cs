using UnityEngine;

/// <summary>可装备契约：定义装备系统挂载/取回物品所需的最小共性能力。
/// 武器与消耗品各自实现本接口即可接入 PlayerEquipmentControl，专属能力（如武器 OverrideController、药壶 ApplyItem）不进接口，由调用方按需向下转型获取。</summary>
public interface IEquippable
{
    /// <summary>物品唯一标识（自动同步为资产文件名）。</summary>
    string ItemID { get; }
    /// <summary>物品图标（装备面板/快捷栏展示用）。</summary>
    Sprite ItemSprite { get; }
    /// <summary>当前是否已被装备。</summary>
    bool IsEquipped { get; set; }
    /// <summary>被装备上时需要挂载的槽位（含是否真正占用）。</summary>
    EquipSlot[] EquipTargetSlots { get; }
    /// <summary>被卸下时需要挂载的槽位（含是否真正占用）。</summary>
    EquipSlot[] UnequipTargetSlots { get; }
    /// <summary>注册持有者（武器/道具由装备系统或拾取系统调用）。</summary>
    void RegisterOwner(Player owner_);
}

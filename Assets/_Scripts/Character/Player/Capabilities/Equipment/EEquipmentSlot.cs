/// <summary>装备挂载位：对应玩家身上的五个武器挂点。
/// 双手武器逻辑占用一对槽位（如 LeftHand+RightHand），由 PlayerEquipmentControl 的 OccupiedSlots 概念处理，枚举本身不变。</summary>
public enum EEquipmentSlot
{
    LeftHand,    //左手槽位
    RightHand,   //右手槽位
    LeftHip,     //左髋槽位
    RightHip,    //右髋槽位
    Back,        //背部槽位
}

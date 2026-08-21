/// <summary>装备切换意图：在动画帧提交前显式记录本次拔刀/收刀动作，避免复用 _slotTaken 占用表当信号位（旧 Toggle 写法会写死 true 致卸下失效）。</summary>
public enum EPendingEquipAction
{
    Equip,
    Unequip,
}

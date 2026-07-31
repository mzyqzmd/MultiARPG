/// <summary>
/// UI 分层：按更新频率划分 Canvas，规避单 Canvas 全量重建的性能陷阱。
/// </summary>
public enum EUILayerCanvas
{
    Static,   //Static=菜单/设置等极低频
    Hud,      //Hud=耐力/血量等高频
    Overlay,  //Overlay=飘字/Toast等中频
}

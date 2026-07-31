using UnityEngine;

/// <summary>
/// 场景可摆放的存档点（MonoBehaviour 触发器，归 World/Interactables/）。
/// 玩家进入触发范围即调用本地 Player 手动存档；数据组装由 Player（数据 owner）负责，
/// 本组件不认识存档数据结构、不依赖 SaveService，只转发请求。仅 owner 端生效。
/// 使用：在 Scene_World 摆放，挂一个 isTrigger 的 Collider，玩家进入即存档。
/// </summary>
public class Checkpoint : MonoBehaviour
{
    #region 玩家进入触发范围 → 手动存档
    private void OnTriggerEnter(Collider other_)
    {
        var player = other_.GetComponent<Player>();
        // 仅本地玩家触发（经 UIService.LocalPlayer 判定，避免读取网络层 IsOwner）
        if (player != null && player == Services.Get<UIService>().LocalPlayer)
        {
            player.ManualSaveCheckpoint();
            Services.Get<UIService>().SpawnUiView<UI_PopText>("PopText", EUILayerCanvas.Overlay)
                .Initialize("已保存存档点", Color.white);
        }
    }
    #endregion
}

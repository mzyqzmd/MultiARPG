using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.AssetRenamer
{
    /// <summary>
    /// 资源重命名钩子：[InitializeOnLoad] 引导确保配置资产存在。
    /// </summary>
    [InitializeOnLoad]
    public static class AssetRenamerHook
    {
        static AssetRenamerHook()
        {
            var _ = SO_AssetRenamerConfig.Instance;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.DependencyGraph
{
    /// <summary>
    /// 依赖关系图钩子：[InitializeOnLoad] 引导确保配置资产存在。
    /// </summary>
    [InitializeOnLoad]
    public static class DependencyGraphHook
    {
        static DependencyGraphHook()
        {
            var _ = SO_DependencyGraphConfig.Instance;
        }
    }
}

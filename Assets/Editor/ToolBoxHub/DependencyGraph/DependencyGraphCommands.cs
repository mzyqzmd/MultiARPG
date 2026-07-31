using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.DependencyGraph
{
    /// <summary>依赖分析结果：正向依赖列表与反向引用列表（均为资产路径）。</summary>
    public sealed class DependencyResult
    {
        public List<string> forward = new List<string>();
        public List<string> reverse = new List<string>();
    }

    /// <summary>
    /// 依赖关系图命令：计算目标资源的正向依赖与反向引用。
    /// </summary>
    public static class DependencyGraphCommands
    {
        #region 分析依赖
        public static DependencyResult Analyze(Object target_)
        {
            var result = new DependencyResult();
            if (target_ == null) return result;
            string path = AssetDatabase.GetAssetPath(target_);
            if (string.IsNullOrEmpty(path)) return result;
            result.forward.AddRange(AssetDatabase.GetDependencies(path, true));
            foreach (var guid in AssetDatabase.FindAssets(""))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p == path) continue;
                if (System.Array.IndexOf(AssetDatabase.GetDependencies(p, false), path) >= 0) result.reverse.Add(p);
            }
            return result;
        }
        #endregion

        // 菜单栏入口
        [MenuItem("ToolBoxHub/查找引用依赖")]
        private static void FindMenu() => DependencyGraphWindow.Open();
    }
}

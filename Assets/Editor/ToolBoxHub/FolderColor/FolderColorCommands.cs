using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.FolderColor
{
    /// <summary>
    /// 染色命令：递归上色 / 清除逻辑 + 右键菜单（纯静态）。
    /// 供 FolderColorWizard 与右键菜单复用；数据持久化走 SO_FolderColorData。
    /// </summary>
    public static class FolderColorCommands
    {
        // 仅文件夹参与染色：父文件夹上色后，用 GetSubFolders 只递归子文件夹；文件直接跳过
        private static void ApplyColorRecursive(string guid_, Color color_)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid_);
            if (!AssetDatabase.IsValidFolder(path)) return; // 非文件夹（文件）不染色
            SO_FolderColorData.Instance.SetColor(guid_, color_);
            foreach (var sub in AssetDatabase.GetSubFolders(path))
                ApplyColorRecursive(AssetDatabase.AssetPathToGUID(sub), color_);
        }

        // 仅文件夹参与清除：与染色对称，只递归子文件夹
        private static void ClearColorRecursive(string guid_)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid_);
            if (!AssetDatabase.IsValidFolder(path)) return;
            SO_FolderColorData.Instance.RemoveColor(guid_);
            foreach (var sub in AssetDatabase.GetSubFolders(path))
                ClearColorRecursive(AssetDatabase.AssetPathToGUID(sub));
        }

        #region 上色 / 清除入口（public，供向导与右键菜单复用）
        public static void ApplyTint(Color color_)
        {
            foreach (var g in Selection.assetGUIDs)
                ApplyColorRecursive(g, color_);
            EditorApplication.RepaintProjectWindow();
        }

        public static void ClearTint()
        {
            foreach (var g in Selection.assetGUIDs)
                ClearColorRecursive(g);
            EditorApplication.RepaintProjectWindow();
        }
        #endregion

        // 右键菜单入口：清除选中项(含子项)染色
        [MenuItem("Assets/FolderColor/清除染色")]
        private static void ClearTintMenu() => ClearTint();
    }
}

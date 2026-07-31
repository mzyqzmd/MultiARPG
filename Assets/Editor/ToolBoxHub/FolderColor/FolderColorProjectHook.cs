using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.FolderColor
{
    /// <summary>
    /// Project 窗口绘制钩子：[InitializeOnLoad] 在编辑器加载即注册，染色后实时显示背景色，与任何窗口开关无关。
    /// 数据持久化统一走 FolderColorData（独立脚本）。
    /// </summary>
    [InitializeOnLoad]
    public static class FolderColorProjectHook
    {
        // 静态构造在编辑器加载/重编译时执行一次；先减后加避免重复注册
        static FolderColorProjectHook()
        {
            EditorApplication.projectWindowItemOnGUI -= DrawProjectItem;
            EditorApplication.projectWindowItemOnGUI += DrawProjectItem;
        }

        // 每项绘制：查映射，有颜色则只染「图标 + 文件夹名」那一块（不染整行）
        private static void DrawProjectItem(string guid_, Rect selectionRect_)
        {
            var color = SO_FolderColorData.Instance.GetColor(guid_);
            if (color == null) return;
            var bg = color.Value;
            bg.a = 0.35f;

            // 一行视图：仅覆盖左侧图标块与名称文本，右侧空白不染
            if (selectionRect_.height <= 22f)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid_);
                string name = path == "Assets" ? "Assets" : Path.GetFileName(path.TrimEnd('/'));
                float iconW = 20f;
                float nameW = GUI.skin.label.CalcSize(new GUIContent(name)).x;
                float w = iconW + nameW + 6f;
                EditorGUI.DrawRect(new Rect(selectionRect_.x, selectionRect_.y, w, selectionRect_.height), bg);
            }
            else
                // 网格视图：整格即标识区，染整块
                EditorGUI.DrawRect(selectionRect_, bg);
        }
    }
}

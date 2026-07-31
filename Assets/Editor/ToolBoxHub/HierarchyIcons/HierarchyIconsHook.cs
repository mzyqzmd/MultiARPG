using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.HierarchyIcons
{
    /// <summary>
    /// 层级窗口绘制钩子：[InitializeOnLoad] 在编辑器加载即注册，按规则在每行右侧绘制组件图标。
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyIconsHook
    {
        static HierarchyIconsHook()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= DrawHierarchyIcons;
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyIcons;
        }

        private static void DrawHierarchyIcons(int instanceID_, Rect selectionRect_)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!SO_HierarchyIconsConfig.Instance.Enabled) return;
            var icons = HierarchyIconsCommands.GetIcons(instanceID_);
            if (icons.Count == 0) return;

            float size = 16f;
            float gap = 4f;
            float rightMargin = 20f;
            float totalW = icons.Count * size + (icons.Count - 1) * gap;
            float x = selectionRect_.xMax - totalW - rightMargin;
            float y = selectionRect_.y + (selectionRect_.height - size) / 2f;
            foreach (var icon in icons)
            {
                if (icon == null) continue;
                GUI.DrawTexture(new Rect(x, y, size, size), icon);
                x += size + gap;
            }
        }
    }
}

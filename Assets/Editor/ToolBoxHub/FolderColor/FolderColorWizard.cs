using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.FolderColor
{
    /// <summary>
    /// 右键染色面板：弹出的临时窗口，常见颜色网格，点击即递归上色。
    /// 复用 FolderColorCommands 的上色入口；数据持久化走 SO_FolderColorData。
    /// </summary>
    public class FolderColorWizard : EditorWindow
    {
        private const int _cols = 6;      // 每行色块数
        private const float _cell = 28f;  // 色块固定宽高，保证网格对齐
        private const float _gap = 6f;    // 色块之间的均匀间距

        // 24 种预设色（静态共享一份），按鲜艳程度：高饱和纯色 → 混合色 → 低饱和/灰阶
        private static readonly Color[] _presets =
        {
            Color.red, new Color(1f, 0.5f, 0f), Color.yellow, Color.green, Color.cyan, Color.blue,
            Color.magenta, new Color(1f, 0f, 0.5f), new Color(0.5f, 0f, 1f), new Color(0.5f, 1f, 0f), new Color(0f, 1f, 0.5f), new Color(0f, 0.5f, 1f),
            new Color(1f, 0.4f, 0.4f), new Color(1f, 0.7f, 0.3f), new Color(1f, 0.9f, 0.4f), new Color(0.6f, 1f, 0.6f), new Color(0.4f, 0.8f, 1f), new Color(0.8f, 0.6f, 1f),
            new Color(0.6f, 0.3f, 0f), new Color(1f, 0.75f, 0.8f), Color.white, new Color(0.75f, 0.75f, 0.75f), Color.gray, Color.black,
        };

        #region 右键菜单入口：弹出浮动面板（自定义颜色）
        public static void Open()
        {
            var w = CreateInstance<FolderColorWizard>();
            w.titleContent = new GUIContent("文件夹染色");
            w.minSize = w.maxSize = new Vector2(210f, 180f);
            w.ShowUtility(); // 浮动工具窗口，符合"临时面板"语义
        }
        #endregion

        [MenuItem("Assets/FolderColor/染色")]
        private static void OpenWizard() => Open();

        // 面板 UI：纯色块网格，点击即染色并关闭
        private void OnGUI()
        {
            EditorGUILayout.LabelField("选择颜色", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(Selection.assetGUIDs.Length == 0))
            {
                for (int i = 0; i < _presets.Length; i++)
                {
                    if (i % _cols == 0)
                    {
                        GUILayout.Space(_gap); // 行间距
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(_gap); // 行首左边距
                    }
                    // 取固定尺寸矩形：整块填色作色块，透明按钮叠在上面接收点击
                    var rect = GUILayoutUtility.GetRect(_cell, _cell, GUILayout.Width(_cell), GUILayout.Height(_cell));
                    EditorGUI.DrawRect(rect, _presets[i]);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    {
                        FolderColorCommands.ApplyTint(_presets[i]);
                        Close();
                    }
                    GUILayout.Space(_gap); // 列间距
                    if (i % _cols == _cols - 1 || i == _presets.Length - 1) EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}

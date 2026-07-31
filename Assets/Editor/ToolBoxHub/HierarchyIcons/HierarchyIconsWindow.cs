using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.HierarchyIcons
{
    /// <summary>
    /// 层级图标规则编辑窗口：全局开关、规则增删与预设应用，改动即时生效。
    /// </summary>
    public class HierarchyIconsWindow : EditorWindow
    {
        private Vector2 _scroll;

        #region 菜单栏入口
        [MenuItem("ToolBoxHub/层级图标")]
        public static void Open() => GetWindow<HierarchyIconsWindow>("层级图标");
        #endregion

        private void OnGUI()
        {
            var config = SO_HierarchyIconsConfig.Instance;
            EditorGUILayout.Space(8);
            config.Enabled = EditorGUILayout.ToggleLeft("启用层级图标", config.Enabled);

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("类型名支持全名（如 UnityEngine.Camera）或短名（如 Camera），留空图标将自动使用组件内置图标。", MessageType.Info);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("组件图标规则", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("启用", GUILayout.Width(28));
            EditorGUILayout.LabelField("组件类型名", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("图标", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("", GUILayout.Width(24));
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var rules = config.GetRules();
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    rule.enabled = EditorGUILayout.ToggleLeft("", rule.enabled, GUILayout.Width(28));
                    rule.ruleName = EditorGUILayout.TextField(rule.ruleName, GUILayout.ExpandWidth(true));
                    rule.icon = (Texture2D)EditorGUILayout.ObjectField(rule.icon, typeof(Texture2D), false, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                    {
                        config.RemoveRule(rule.ruleName);
                        break;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用常用预设"))
                config.ApplyPresets();
            if (GUILayout.Button("+ 添加规则"))
                config.AddRule("", null);
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                config.Save();
                HierarchyIconsCommands.ClearCache();
            }
        }
    }
}

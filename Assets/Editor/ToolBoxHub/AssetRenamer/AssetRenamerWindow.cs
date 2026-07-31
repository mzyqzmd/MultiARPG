using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.AssetRenamer
{
    /// <summary>
    /// 资源批量重命名窗口：配置规则、预览冲突并执行重命名（可停靠）。
    /// </summary>
    public class AssetRenamerWindow : EditorWindow
    {
        #region 菜单栏入口：可停靠窗口
        public static void Open() => GetWindow<AssetRenamerWindow>("批量重命名");
        #endregion

        private List<RenamePreview> _preview = new List<RenamePreview>();
        private Vector2 _scroll;

        private void OnEnable() => RefreshPreview();

        private void OnGUI()
        {
            var cfg = SO_AssetRenamerConfig.Instance;
            string prefix = EditorGUILayout.TextField("前缀", cfg.Prefix);
            string suffix = EditorGUILayout.TextField("后缀", cfg.Suffix);
            string find = EditorGUILayout.TextField("查找", cfg.Find);
            string replace = EditorGUILayout.TextField("替换为", cfg.Replace);
            bool useNum = EditorGUILayout.Toggle("启用序号", cfg.UseNumbering);
            int start = useNum ? EditorGUILayout.IntField("起始序号", cfg.StartNumber) : cfg.StartNumber;
            ECaseMode caseMode = (ECaseMode)EditorGUILayout.EnumPopup("大小写", cfg.CaseMode);
            if (GUI.changed) cfg.Set(prefix, suffix, find, replace, useNum, start, caseMode);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("预览")) RefreshPreview();
            if (GUILayout.Button("执行重命名") && _preview.Count > 0)
            {
                AssetRenamerCommands.Apply(_preview);
                RefreshPreview();
            }
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var p in _preview)
            {
                var label = $"{p.oldName}  →  {p.newName}";
                if (p.conflict) label += "  [冲突]";
                EditorGUILayout.LabelField(label, p.conflict ? EditorStyles.boldLabel : EditorStyles.label);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RefreshPreview()
        {
            var cfg = SO_AssetRenamerConfig.Instance;
            _preview = AssetRenamerCommands.Preview(Selection.objects, cfg.Prefix, cfg.Suffix, cfg.Find, cfg.Replace, cfg.UseNumbering, cfg.StartNumber, cfg.CaseMode);
        }
    }
}

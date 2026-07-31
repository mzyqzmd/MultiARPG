using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.DependencyGraph
{
    /// <summary>
    /// 依赖关系图窗口：分析选中资源的正向依赖与反向引用，点击可定位（可停靠）。
    /// </summary>
    public class DependencyGraphWindow : EditorWindow
    {
        #region 菜单栏入口：可停靠窗口
        public static void Open() => GetWindow<DependencyGraphWindow>("依赖关系图");
        #endregion

        private Object _target;
        private DependencyResult _result;
        private Vector2 _scrollF;
        private Vector2 _scrollR;

        private void OnEnable() => _result = DependencyGraphCommands.Analyze(Selection.activeObject);

        private void OnGUI()
        {
            _target = EditorGUILayout.ObjectField("目标资源", _target, typeof(Object), false);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("分析")) _result = DependencyGraphCommands.Analyze(_target);
            if (GUILayout.Button("分析选中")) _result = DependencyGraphCommands.Analyze(Selection.activeObject);
            EditorGUILayout.EndHorizontal();

            int f = _result == null ? 0 : _result.forward.Count;
            int r = _result == null ? 0 : _result.reverse.Count;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField($"正向依赖（{f}）", EditorStyles.boldLabel);
            _scrollF = EditorGUILayout.BeginScrollView(_scrollF);
            if (_result != null)
                foreach (var p in _result.forward)
                    if (GUILayout.Button(System.IO.Path.GetFileName(p), GUILayout.ExpandWidth(true))) Ping(p);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField($"反向引用（{r}）", EditorStyles.boldLabel);
            _scrollR = EditorGUILayout.BeginScrollView(_scrollR);
            if (_result != null)
                foreach (var p in _result.reverse)
                    if (GUILayout.Button(System.IO.Path.GetFileName(p), GUILayout.ExpandWidth(true))) Ping(p);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void Ping(string path_)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(path_);
            if (obj != null) EditorGUIUtility.PingObject(obj);
        }
    }
}

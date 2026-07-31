using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.HierarchyIcons
{
    /// <summary>
    /// 图标解析与缓存：将规则解析为组件类型并取内置/自定义图标，供 Hook 绘制前查询。
    /// </summary>
    public static class HierarchyIconsCommands
    {
        private static Dictionary<int, List<Texture>> _objectCache = new();
        private static Dictionary<string, Type> _typeCache = new();

        static HierarchyIconsCommands()
        {
            EditorApplication.hierarchyChanged -= ClearCache;
            EditorApplication.hierarchyChanged += ClearCache;
            // 域重载后清空全部缓存并启动预热重绘，直到图标全部就绪(自愈)
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterReload;
        }

        // 重编译后内置图标(AssetPreview/ObjectContent)需要预热，期间持续重绘层级窗口
        // 直到所有类型图标解析完成；_pendingResolve 为 false 即停止，最多 _repaintBudget 帧防呆
        private static bool _pendingResolve;
        private static bool _warming;
        private static int _repaintBudget;

        private static void OnAfterReload()
        {
            // 域重载后旧 Type/纹理引用失效，全部清空重新解析
            _objectCache.Clear();
            _typeCache.Clear();
            _typeIconCache.Clear();
            _pendingResolve = true;
            StartWarmup(300);
        }

        // 预热重绘：反复重绘层级窗口直到图标就绪
        private static void StartWarmup(int frames_)
        {
            if (_warming) return;
            _warming = true;
            _repaintBudget = frames_;
            EditorApplication.update -= RepaintTick;
            EditorApplication.update += RepaintTick;
        }

        private static void RepaintTick()
        {
            // 读取上一帧重绘时 Compute 是否仍发现未就绪图标
            bool pending = _pendingResolve;
            _pendingResolve = false; // 等待本帧重绘中重新置位
            EditorApplication.RepaintHierarchyWindow();
            if (!pending || --_repaintBudget <= 0)
            {
                EditorApplication.update -= RepaintTick;
                _warming = false;
            }
        }

        #region 图标缓存与查询
        public static void ClearCache()
        {
            _objectCache.Clear();
            _typeCache.Clear();
        }

        public static List<Texture> GetIcons(int instanceID_)
        {
            if (_objectCache.TryGetValue(instanceID_, out var list)) return list;
            Compute(instanceID_, out list, out var complete);
            // 仅当无图标待解析时才缓存，保证未就绪的预设图标会持续重试
            if (complete && list.Count > 0) _objectCache[instanceID_] = list;
            if (_pendingResolve) StartWarmup(300);
            return list;
        }
        #endregion

        // 类型 -> 图标 解析缓存，避免重复查询
        private static readonly Dictionary<Type, Texture> _typeIconCache = new();

        private static void Compute(int instanceID_, out List<Texture> list_, out bool complete_)
        {
            list_ = new List<Texture>();
            complete_ = true;
            var obj = EditorUtility.InstanceIDToObject(instanceID_) as GameObject;
            if (obj == null) return;
            foreach (var rule in SO_HierarchyIconsConfig.Instance.GetRules())
            {
                if (!rule.enabled) continue;
                var type = ResolveType(rule.ruleName);
                if (type == null) continue;
                var comp = obj.GetComponent(type);
                if (comp == null) continue;
                // rule.icon 可能是 missing 对象：引用非 null 但对象无效，需用 Unity 的 bool 重载判断
                var tex = rule.icon ? rule.icon : GetTypeIcon(comp, type);
                if (tex != null) list_.Add(tex);
                else complete_ = false; // 该对象仍有图标未就绪，暂不缓存以便后续重试
            }
        }

        // 预设规则 icon 为空时取组件类型图标：Unity 内部 GetIconForObject 最可靠
        // (Inspector 组件标题即用它)；取不到则依次用 ObjectContent 实例/AssetPreview/ObjectContent(null)
        private static readonly Func<UnityEngine.Object, Texture> _getIconForObject = CreateGetIconForObject();
        private static readonly HashSet<Type> _missingIconLog = new();

        private static Func<UnityEngine.Object, Texture> CreateGetIconForObject()
        {
            try
            {
                foreach (var type in new[] { typeof(EditorGUIUtility), typeof(Editor) })
                {
                    foreach (var m in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        if (m.Name != "GetIconForObject") continue;
                        var ps = m.GetParameters();
                        if (ps.Length != 1 || ps[0].ParameterType != typeof(UnityEngine.Object)) continue;
                        if (!typeof(Texture).IsAssignableFrom(m.ReturnType)) continue;
                        return (Func<UnityEngine.Object, Texture>)Delegate.CreateDelegate(typeof(Func<UnityEngine.Object, Texture>), m);
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning($"[HierarchyIcons] GetIconForObject 反射失败: {e.Message}"); }
            return null;
        }

        private static Texture GetTypeIcon(Component comp_, Type type_)
        {
            if (_typeIconCache.TryGetValue(type_, out var cached)) return cached;

            var tex = _getIconForObject?.Invoke(comp_);
            if (tex != null) { _typeIconCache[type_] = tex; return tex; }

            tex = EditorGUIUtility.ObjectContent(comp_, type_)?.image;
            if (tex != null) { _typeIconCache[type_] = tex; return tex; }

            tex = AssetPreview.GetMiniTypeThumbnail(type_);
            if (tex != null) { _typeIconCache[type_] = tex; return tex; }

            tex = EditorGUIUtility.ObjectContent(null, type_)?.image;
            if (tex != null) { _typeIconCache[type_] = tex; return tex; }

            if (_missingIconLog.Add(type_))
                Debug.LogWarning($"[HierarchyIcons] 无法获取 {type_.Name} 的内置图标，内部API={_getIconForObject != null}");
            _pendingResolve = true; // 本类型图标暂未就绪，标记需继续预热
            return null;
        }

        private static Type ResolveType(string typeName_)
        {
            if (_typeCache.TryGetValue(typeName_, out var cached)) return cached;

            // 先尝试从常见程序集直接获取全名，避免 Type.GetType("Camera") 搜不到 UnityEngine.Camera
            var t = Type.GetType($"UnityEngine.{typeName_}, UnityEngine") ?? Type.GetType($"UnityEngine.{typeName_}, UnityEngine.CoreModule") ?? Type.GetType(typeName_);
            if (t != null)
            {
                _typeCache[typeName_] = t;
                return t;
            }

            foreach (var c in TypeCache.GetTypesDerivedFrom<Component>())
            {
                if (c.FullName != typeName_ && c.Name != typeName_) continue;
                t = c;
                break;
            }

            if (t != null) _typeCache[typeName_] = t;
            return t;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.DependencyGraph
{
    /// <summary>
    /// 依赖关系图配置：懒加载单例（配置资产确保存在，供钩子引导）。
    /// </summary>
    public class SO_DependencyGraphConfig : ScriptableObject
    {
        private const string _assetPath = "Assets/Editor/ToolBoxHub/DependencyGraph/SO_DependencyGraphConfig.asset";

        private static SO_DependencyGraphConfig _instance;
        public static SO_DependencyGraphConfig Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = AssetDatabase.LoadAssetAtPath<SO_DependencyGraphConfig>(_assetPath);
                if (_instance) return _instance;
                _instance = CreateInstance<SO_DependencyGraphConfig>();
                AssetDatabase.CreateAsset(_instance, _assetPath);
                AssetDatabase.SaveAssets();
                return _instance;
            }
        }
    }
}

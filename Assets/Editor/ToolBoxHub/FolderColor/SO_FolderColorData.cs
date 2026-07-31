using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.FolderColor
{
    /// <summary>
    /// 颜色映射数据（GUID -> 颜色）持久化资产。
    /// 继承 ScriptableObject，按命名规则以 SO_ 开头。
    /// 用经典 ScriptableObject + AssetDatabase 懒加载（兼容所有 Unity 版本，不依赖 ScriptableSingleton）。
    /// </summary>
    public class SO_FolderColorData : ScriptableObject
    {
        // 资产存放路径（放在本工具目录下）
        private const string _assetPath = "Assets/Editor/ToolBoxHub/FolderColor/FolderColorData.asset";

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        [System.Serializable]
        private class Entry
        {
            public string guid;
            public string colorHtml; // 形如 "#RRGGBB"
        }

        // 懒加载单例：资产不存在就现场创建一个
        private static SO_FolderColorData _instance;
        public static SO_FolderColorData Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = AssetDatabase.LoadAssetAtPath<SO_FolderColorData>(_assetPath);
                if (_instance) return _instance;
                _instance = CreateInstance<SO_FolderColorData>();
                AssetDatabase.CreateAsset(_instance, _assetPath);
                AssetDatabase.SaveAssets();
                return _instance;
            }
        }

        #region 读取某 GUID 的颜色（无记录返回 null）
        public Color? GetColor(string guid_)
        {
            var e = _entries.Find(x => x.guid == guid_);
            if (e == null) return null;
            return ColorUtility.TryParseHtmlString(e.colorHtml, out var c) ? (Color?)c : null;
        }
        #endregion

        #region 写入 / 清除某 GUID 的颜色并保存
        public void SetColor(string guid_, Color color_)
        {
            var e = _entries.Find(x => x.guid == guid_);
            var html = "#" + ColorUtility.ToHtmlStringRGB(color_);
            if (e == null) _entries.Add(new Entry { guid = guid_, colorHtml = html });
            else e.colorHtml = html;
            Save();
        }

        public void RemoveColor(string guid_)
        {
            _entries.RemoveAll(x => x.guid == guid_);
            Save();
        }
        #endregion

        // 标记脏并写回磁盘
        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}

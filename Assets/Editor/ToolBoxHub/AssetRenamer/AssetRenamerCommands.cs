using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.AssetRenamer
{
    /// <summary>重命名时的大小写处理模式。</summary>
    public enum ECaseMode { None, Upper, Lower }

    /// <summary>单条资源的重命名预览：资产引用、旧名、新名及是否冲突。</summary>
    public sealed class RenamePreview
    {
        public Object asset;
        public string oldName;
        public string newName;
        public bool conflict;
    }

    /// <summary>
    /// 资源重命名命令：按前缀/后缀/查找替换/序号/大小写生成预览并执行重命名。
    /// </summary>
    public static class AssetRenamerCommands
    {
        #region 生成预览
        public static List<RenamePreview> Preview(Object[] assets_, string prefix_, string suffix_, string find_, string replace_, bool useNumbering_, int startNumber_, ECaseMode caseMode_)
        {
            var list = new List<RenamePreview>();
            int n = startNumber_;
            foreach (var obj in assets_)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                string oldName = string.IsNullOrEmpty(path) ? obj.name : Path.GetFileNameWithoutExtension(path);
                string name = string.IsNullOrEmpty(find_) ? oldName : oldName.Replace(find_, replace_);
                if (useNumbering_) name = $"{name}_{n:D4}";
                if (!string.IsNullOrEmpty(prefix_)) name = prefix_ + name;
                if (!string.IsNullOrEmpty(suffix_)) name = name + suffix_;
                if (caseMode_ == ECaseMode.Upper) name = name.ToUpper();
                else if (caseMode_ == ECaseMode.Lower) name = name.ToLower();
                list.Add(new RenamePreview { asset = obj, oldName = oldName, newName = name });
                n++;
            }
            for (int i = 0; i < list.Count; i++)
                for (int j = 0; j < list.Count; j++)
                    if (i != j && list[i].newName == list[j].newName) list[i].conflict = true;
            return list;
        }
        #endregion

        #region 执行重命名
        public static void Apply(List<RenamePreview> previews_)
        {
            foreach (var p in previews_)
            {
                if (p.conflict) continue;
                string path = AssetDatabase.GetAssetPath(p.asset);
                if (string.IsNullOrEmpty(path)) p.asset.name = p.newName;
                else AssetDatabase.RenameAsset(path, p.newName);
            }
            AssetDatabase.SaveAssets();
        }
        #endregion

        // 菜单栏入口
        [MenuItem("ToolBoxHub/批量重命名")]
        private static void RenameMenu() => AssetRenamerWindow.Open();
    }
}

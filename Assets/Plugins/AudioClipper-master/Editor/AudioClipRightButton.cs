using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AudioClipRightButton
{
    static AudioClipRightButton()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
    }

    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        // 通过GUID获取对应的资源
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));

        // 只对AudioClip类型的资源生效
        if (asset is AudioClip audioClip)
        {
            // 检查当前资源是否被选中
            bool isSelected = Selection.objects != null && System.Array.IndexOf(Selection.objects, asset) != -1;

            if (isSelected)
            {
                // 计算按钮位置（在资源项右侧）
                float buttonWidth = 80;
                Rect buttonRect = new Rect(
                    selectionRect.x + selectionRect.width - buttonWidth - 5, // 右侧偏移
                    selectionRect.y + 2,
                    buttonWidth,
                    selectionRect.height - 4
                );

                // 绘制按钮
               /* if (GUI.Button(buttonRect, "处理音频"))
                {
                    // 按钮点击逻辑
                    HandleAudioClip(audioClip);
                }*/
            }
        }
    }

    /*private static void HandleAudioClip(AudioClip clip)
    {
        AudioClipper clipper = Resources.Load<AudioClipper>("AudioClipper");
        if (clipper != null)
        {
            if (clipper.ClipConfigs == null)
            {
                clipper.ClipConfigs = new AudioClipConfig[0];
            }

            AudioClipConfig config = new AudioClipConfig
            {
                ClipSource = clip
            };

            var configs = clipper.ClipConfigs.ToList();
            configs.Add(config);
            clipper.ClipConfigs = configs.ToArray();

            Selection.activeObject = clipper;
            EditorGUIUtility.PingObject(clipper);
        }
        else
        {
            Debug.LogError("未找到AudioClipper的配置文件，请检查【AudioClipper/Resources】中是否存在AudioClipper的配置文件或者配置名称是否为【AudioClipper】");
        }

    }*/

}

using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using System.Linq;

[CustomEditor(typeof(AudioClipper))]
public class AudioClipperEditor : Editor
{
    // 序列化属性
    private SerializedProperty _clipConfigsProp;
    // 反射方法
    private MethodInfo _getPreviewPosMethod;
    private MethodInfo _playPreviewMethod;
    private MethodInfo _setPreviewPosMethod;
    private MethodInfo _stopAllPreviewMethod;
    // 波形数据缓存
    private readonly Dictionary<AudioClip, float[]> _clipSampleCache = new();
    // 当前播放状态
    private int _currentPlayingIndex = -1;
    private EditorApplication.CallbackFunction _previewUpdateCallback;


    #region 初始化
    private void OnEnable()
    {
        // 初始化序列化属性
        _clipConfigsProp = serializedObject.FindProperty("ClipConfigs");
        // 初始化反射方法
        InitAudioUtilReflection();
    }

    private void OnDisable()
    {
        // 停止播放并清理资源
        StopCurrentPreview();
        _clipSampleCache.Clear();
    }
    #endregion


    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript(); // 确保序列化数据最新
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("音频裁剪配置", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 处理配置列表显示
        DrawClipConfigList();

        // 添加新配置按钮
        EditorGUILayout.Space(8);
        if (GUILayout.Button("添加新音频配置", GUILayout.Height(25)))
        {
            AddNewClipConfig();
        }

        // 提交序列化数据修改
        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
        }
    }


    #region Inspector显示
    /// <summary>
    /// 绘制音频配置列表
    /// </summary>
    private void DrawClipConfigList()
    {
        if (_clipConfigsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("暂无音频配置，点击下方按钮添加", MessageType.Info);
            return;
        }

        // 循环绘制每个配置项
        for (int i = 0; i < _clipConfigsProp.arraySize; i++)
        {
            SerializedProperty configProp = _clipConfigsProp.GetArrayElementAtIndex(i);
            DrawSingleClipConfig(configProp, i);
            EditorGUILayout.Space(5);
        }
    }

    /// <summary>
    /// 绘制单个音频配置项
    /// </summary>
    private void DrawSingleClipConfig(SerializedProperty configProp, int index)
    {
        // 配置项容器
        using (new EditorGUILayout.VerticalScope("box"))
        {
            // 配置项标题 + 删除按钮
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"配置项 {index + 1}", EditorStyles.miniBoldLabel);
                // 删除按钮
                if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(18)))
                {
                    if (EditorUtility.DisplayDialog("提示", "确定删除该音频配置吗？", "确定", "取消"))
                    {
                        // 若删除的是正在播放的配置，先停止播放
                        if (index == _currentPlayingIndex)
                        {
                            StopCurrentPreview();
                        }
                        _clipConfigsProp.DeleteArrayElementAtIndex(index);
                        return; // 重新绘制列表，避免索引异常
                    }
                }
            }

            // 音频源选择
            SerializedProperty clipSourceProp = configProp.FindPropertyRelative("ClipSource");
            clipSourceProp.objectReferenceValue = EditorGUILayout.ObjectField(
                "音频文件",
                clipSourceProp.objectReferenceValue,
                typeof(AudioClip),
                false
            );

            // 若未选择音频，显示提示并跳过后续绘制
            if (clipSourceProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("请选择一个AudioClip文件", MessageType.Warning);
                return;
            }

            // 音频基础信息
            AudioClip clip = clipSourceProp.objectReferenceValue as AudioClip;
            SerializedProperty clipLengthProp = configProp.FindPropertyRelative("ClipLengthMs");
            clipLengthProp.floatValue = clip.length * 1000f; // 同步毫秒长度
            EditorGUILayout.LabelField(
                "音频总长度",
                $"{clipLengthProp.floatValue:F0} ms ({clip.length:F2} s)"
            );

            // 输入框 + 滑动条
            SerializedProperty startTimeProp = configProp.FindPropertyRelative("StartTimeMs");
            SerializedProperty endTimeProp = configProp.FindPropertyRelative("EndTimeMs");

            // 边界修正
            float maxLength = clipLengthProp.floatValue;
            startTimeProp.floatValue = Mathf.Clamp(startTimeProp.floatValue, 0, maxLength);
            endTimeProp.floatValue = Mathf.Clamp(endTimeProp.floatValue, startTimeProp.floatValue + 10, maxLength); // 最小10ms

            // 时间输入框
            using (new EditorGUILayout.HorizontalScope())
            {
                startTimeProp.floatValue = EditorGUILayout.FloatField("起始时间 (ms)", startTimeProp.floatValue);
                endTimeProp.floatValue = EditorGUILayout.FloatField("结束时间 (ms)", endTimeProp.floatValue);
            }
            // 显示片段时长
            EditorGUILayout.LabelField(
                $"时长: {endTimeProp.floatValue - startTimeProp.floatValue:F0} ms",
                GUILayout.ExpandWidth(true)
            );


            // 波形图绘制
            Rect waveformRect = GUILayoutUtility.GetRect(
                0,
                80,
                GUILayout.ExpandWidth(true)
            );
            DrawWaveform(
                clip,
                maxLength,
                startTimeProp.floatValue,
                endTimeProp.floatValue,
                configProp.FindPropertyRelative("PreviewPositionMs").floatValue,
                index == _currentPlayingIndex,
                waveformRect
            );
            // 时间滑动条
            float startTime = startTimeProp.floatValue;
            float endTime = endTimeProp.floatValue;
            EditorGUILayout.MinMaxSlider(
                ref startTime,
                ref endTime,
                0,
                maxLength
            );
            startTimeProp.floatValue = startTime;
            endTimeProp.floatValue = endTime;
            // 播放/停止 + 导出，横向排列
            using (new EditorGUILayout.HorizontalScope())
            {
                bool isPlaying = index == _currentPlayingIndex;
                if (GUILayout.Button(
                    isPlaying ? "停止试听" : "试听片段",
                    GUILayout.Height(20)
                ))
                {
                    if (isPlaying)
                    {
                        StopCurrentPreview();
                    }
                    else
                    {
                        StartPreview(configProp, clip, index);
                    }
                }

                if (GUILayout.Button("导出WAV", GUILayout.Height(20)))
                {
                    ExportClip(clip, startTimeProp.floatValue, endTimeProp.floatValue);
                }
            }
        }
    }

    /// <summary>
    /// 添加新的音频配置项
    /// </summary>
    private void AddNewClipConfig()
    {
        int newIndex = _clipConfigsProp.arraySize;
        _clipConfigsProp.InsertArrayElementAtIndex(newIndex);
        SerializedProperty newConfig = _clipConfigsProp.GetArrayElementAtIndex(newIndex);

        // 初始化默认值
        newConfig.FindPropertyRelative("StartTimeMs").floatValue = 0;
        newConfig.FindPropertyRelative("EndTimeMs").floatValue = 1000; // 默认1秒
        newConfig.FindPropertyRelative("isPlaying").boolValue = false;
        newConfig.FindPropertyRelative("PreviewPositionMs").floatValue = 0;
    }
    #endregion


    #region 波形图绘制
    /// <summary>
    /// 绘制音频波形
    /// </summary>
    private void DrawWaveform(AudioClip clip, float totalMs, float startMs, float endMs, float previewMs, bool isPlaying, Rect rect)
    {
        // 缓存音频样本数据
        if (!_clipSampleCache.ContainsKey(clip))
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            _clipSampleCache[clip] = samples;
        }
        float[] samplesData = _clipSampleCache[clip];

        // 绘制背景和边框
        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.9f, 0.9f, 0.9f));
        Rect borderRect = rect;
        borderRect.width -= 1;
        borderRect.height -= 1;
        EditorGUI.DrawRect(borderRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));

        // 计算绘制参数
        int pixelWidth = Mathf.RoundToInt(rect.width);
        float msPerPixel = totalMs / pixelWidth;
        int samplesPerPixel = Mathf.Max(1, (int)(clip.frequency * clip.channels * msPerPixel / 1000f));

        // 绘制波形线
        Color waveColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.8f, 0.3f) : new Color(0, 0.6f, 0);
        Handles.color = waveColor;
        int centerY = (int)(rect.y + rect.height / 2);
        float heightScale = rect.height / 2f * 0.85f; // 留边距，避免溢出

        for (int x = 0; x < pixelWidth; x++)
        {
            // 计算当前像素对应的样本索引
            float currentMs = x * msPerPixel;
            int sampleIndex = (int)(currentMs / 1000f * clip.frequency * clip.channels);
            if (sampleIndex >= samplesData.Length) break;

            // 取当前像素范围内的最大振幅
            float maxAmp = 0;
            for (int i = 0; i < samplesPerPixel && sampleIndex + i < samplesData.Length; i++)
            {
                maxAmp = Mathf.Max(maxAmp, Mathf.Abs(samplesData[sampleIndex + i]));
            }

            // 绘制单条波形线
            int xPos = (int)(rect.x + x);
            int topY = (int)(centerY - maxAmp * heightScale);
            int bottomY = (int)(centerY + maxAmp * heightScale);
            Handles.DrawLine(new Vector2(xPos, topY), new Vector2(xPos, bottomY));
        }

        // 绘制裁剪区域高亮
        float widthPerMs = rect.width / totalMs;
        float startX = rect.x + startMs * widthPerMs;
        float endX = rect.x + endMs * widthPerMs;
        EditorGUI.DrawRect(
            new Rect(startX, rect.y, endX - startX, rect.height),
            new Color(0.5f, 0.7f, 1f, 0.2f)
        );

        // 绘制时间刻度
        DrawTimeMarkers(rect, totalMs);

        // 绘制标记线（起始-青色，结束-粉红，播放头-黄色）
        DrawMarkerLine(rect, startMs, totalMs, Color.cyan);
        DrawMarkerLine(rect, endMs, totalMs, Color.magenta);
        if (isPlaying)
        {
            DrawMarkerLine(rect, previewMs, totalMs, Color.yellow);
        }
    }

    /// <summary>
    /// 绘制时间刻度
    /// </summary>
    private void DrawTimeMarkers(Rect rect, float totalMs)
    {
        Handles.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        GUIStyle labelStyle = new(EditorStyles.label)
        {
            fontSize = 8,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        // 自动计算刻度间隔
        float interval = totalMs switch
        {
            < 1000 => 100,
            < 5000 => 500,
            < 10000 => 1000,
            < 30000 => 5000,
            _ => 10000
        };

        // 绘制刻度线和标签
        for (float t = 0; t <= totalMs; t += interval)
        {
            float x = rect.x + (t / totalMs) * rect.width;
            // 刻度线
            Handles.DrawLine(
                new Vector2(x, rect.y + rect.height - 1),
                new Vector2(x, rect.y + rect.height - 5)
            );
            // 时间标签
            if (x > rect.x + 10 && x < rect.x + rect.width - 10)
            {
                GUI.Label(
                    new Rect(x - 15, rect.y + rect.height - 12, 30, 10),
                    $"{t:F0}ms",
                    labelStyle
                );
            }
        }
    }

    /// <summary>
    /// 绘制标记线（起始/结束/播放头）
    /// </summary>
    private void DrawMarkerLine(Rect rect, float targetMs, float totalMs, Color color)
    {
        float x = rect.x + (targetMs / totalMs) * rect.width;
        Handles.color = color;
        Handles.DrawLine(
            new Vector2(x, rect.y),
            new Vector2(x, rect.y + rect.height)
        );
    }
    #endregion


    #region 音频播放
    /// <summary>
    /// 初始化Unity内部AudioUtil反射方法
    /// </summary>
    private void InitAudioUtilReflection()
    {
        try
        {
            Assembly editorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilType = editorAssembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilType == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到UnityEditor.AudioUtil（可能版本不兼容）", "确定");
                return;
            }

            // 缓存所需反射方法
            _playPreviewMethod = audioUtilType.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );
            _setPreviewPosMethod = audioUtilType.GetMethod(
                "SetPreviewClipSamplePosition",
                BindingFlags.Static | BindingFlags.Public
            );
            _getPreviewPosMethod = audioUtilType.GetMethod(
                "GetPreviewClipSamplePosition",
                BindingFlags.Static | BindingFlags.Public
            );
            _stopAllPreviewMethod = audioUtilType.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public
            );
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("反射初始化失败", ex.Message, "确定");
        }
    }

    /// <summary>
    /// 开始预览指定配置的音频片段
    /// </summary>
    private void StartPreview(SerializedProperty configProp, AudioClip clip, int index)
    {
        // 先停止当前播放（避免多音频同时播放）
        StopCurrentPreview();

        // 检查反射方法是否可用
        if (_playPreviewMethod == null || _setPreviewPosMethod == null)
        {
            EditorUtility.DisplayDialog("错误", "音频播放功能初始化失败", "确定");
            return;
        }

        // 获取配置的时间参数
        float startMs = configProp.FindPropertyRelative("StartTimeMs").floatValue;
        float endMs = configProp.FindPropertyRelative("EndTimeMs").floatValue;
        float startSec = startMs / 1000f;
        float durationSec = (endMs - startMs) / 1000f;

        try
        {
            // 开始播放预览
            _playPreviewMethod.Invoke(null, new object[] { clip, 0, false });
            // 设置播放起始位置
            int startSample = (int)(startSec * clip.frequency);
            _setPreviewPosMethod.Invoke(null, new object[] { clip, startSample });

            // 初始化播放状态
            _currentPlayingIndex = index;
            configProp.FindPropertyRelative("isPlaying").boolValue = true;
            configProp.FindPropertyRelative("PlayStartTime").floatValue = Time.realtimeSinceStartup;
            configProp.FindPropertyRelative("PreviewPositionMs").floatValue = startMs;

            // 注册预览更新回调（实时更新播放头）
            _previewUpdateCallback = UpdatePreview;
            EditorApplication.update += _previewUpdateCallback;

            // 自动停止计时（精确到毫秒）
            EditorApplication.delayCall += () =>
            {
                double stopTime = EditorApplication.timeSinceStartup + durationSec;
                EditorApplication.CallbackFunction stopCallback = null;
                stopCallback = () =>
                {
                    if (EditorApplication.timeSinceStartup >= stopTime || _currentPlayingIndex != index)
                    {
                        StopCurrentPreview();
                        EditorApplication.update -= stopCallback;
                    }
                    else
                    {
                        EditorApplication.delayCall += stopCallback;
                    }
                };
                EditorApplication.delayCall += stopCallback;
            };
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("播放失败", $"错误信息：{ex.InnerException?.Message}", "确定");
            StopCurrentPreview();
        }
    }

    /// <summary>
    /// 更新预览状态（播放头位置、进度同步）
    /// </summary>
    private void UpdatePreview()
    {
        if (_currentPlayingIndex < 0 || _currentPlayingIndex >= _clipConfigsProp.arraySize)
        {
            StopCurrentPreview();
            return;
        }

        SerializedProperty configProp = _clipConfigsProp.GetArrayElementAtIndex(_currentPlayingIndex);
        AudioClip clip = configProp.FindPropertyRelative("ClipSource").objectReferenceValue as AudioClip;
        if (clip == null)
        {
            StopCurrentPreview();
            return;
        }

        // 获取配置参数
        float startMs = configProp.FindPropertyRelative("StartTimeMs").floatValue;
        float endMs = configProp.FindPropertyRelative("EndTimeMs").floatValue;
        float playStartTime = configProp.FindPropertyRelative("PlayStartTime").floatValue;
        SerializedProperty previewPosProp = configProp.FindPropertyRelative("PreviewPositionMs");

        // 计算理论播放位置（基于系统时间）
        float elapsedMs = (Time.realtimeSinceStartup - playStartTime) * 1000f;
        float currentPreviewMs = startMs + elapsedMs;

        // 用反射获取实际播放位置（校正理论误差）
        if (_getPreviewPosMethod != null)
        {
            try
            {
                int actualSample = (int)_getPreviewPosMethod.Invoke(null, new object[] { clip });
                float actualPreviewMs = (actualSample / (float)clip.frequency) * 1000f;
                // 误差超过50ms时校正（避免频繁抖动）
                if (Mathf.Abs(actualPreviewMs - currentPreviewMs) > 50)
                {
                    currentPreviewMs = actualPreviewMs;
                    configProp.FindPropertyRelative("PlayStartTime").floatValue = Time.realtimeSinceStartup - (currentPreviewMs - startMs) / 1000f;
                }
            }
            catch { /* 反射失败时使用理论位置 */ }
        }

        // 更新播放位置（超出范围则停止）
        if (currentPreviewMs >= endMs)
        {
            StopCurrentPreview();
            return;
        }
        previewPosProp.floatValue = currentPreviewMs;
        serializedObject.ApplyModifiedPropertiesWithoutUndo(); // 不记录撤销，避免干扰用户操作

        // 强制刷新Inspector（确保播放头实时更新）
        Repaint();
    }

    /// <summary>
    /// 停止当前正在播放的音频预览
    /// </summary>
    private void StopCurrentPreview()
    {
        // 停止播放（调用Unity内部方法）
        if (_stopAllPreviewMethod != null)
        {
            try { _stopAllPreviewMethod.Invoke(null, null); }
            catch { /* 忽略停止失败（可能已停止） */ }
        }

        // 清理回调
        if (_previewUpdateCallback != null)
        {
            EditorApplication.update -= _previewUpdateCallback;
            _previewUpdateCallback = null;
        }

        // 重置当前播放配置的状态
        if (_currentPlayingIndex >= 0 && _currentPlayingIndex < _clipConfigsProp.arraySize)
        {
            SerializedProperty configProp = _clipConfigsProp.GetArrayElementAtIndex(_currentPlayingIndex);
            configProp.FindPropertyRelative("isPlaying").boolValue = false;
            configProp.FindPropertyRelative("PreviewPositionMs").floatValue = configProp.FindPropertyRelative("StartTimeMs").floatValue;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        // 重置播放索引
        _currentPlayingIndex = -1;
    }
    #endregion


    #region 音频导出
    private void ExportClip(AudioClip clip, float startMs, float endMs)
    {
        // 合法性校验
        if (clip == null)
        {
            EditorUtility.DisplayDialog("错误", "音频文件为空", "确定");
            return;
        }
        if (endMs <= startMs)
        {
            EditorUtility.DisplayDialog("错误", "结束时间必须大于起始时间", "确定");
            return;
        }

        // 转换时间单位（毫秒→秒→样本）
        float startSec = Mathf.Clamp(startMs / 1000f, 0, clip.length);
        float endSec = Mathf.Clamp(endMs / 1000f, 0, clip.length);
        int frequency = clip.frequency;
        int channels = clip.channels;
        int startSample = (int)(startSec * frequency) * channels;
        int endSample = (int)(endSec * frequency) * channels;
        int sampleCount = endSample - startSample;

        if (sampleCount <= 0)
        {
            EditorUtility.DisplayDialog("错误", "无效的音频片段（样本数为0）", "确定");
            return;
        }

        // 读取音频数据
        float[] sampleData = new float[sampleCount];
        try
        {
            clip.GetData(sampleData, startSample / channels);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("错误", $"读取音频数据失败：{ex.Message}", "确定");
            return;
        }

        // 创建裁剪后的音频
        AudioClip newClip = AudioClip.Create(
            $"{clip.name}_{startMs}ms-{endMs}ms",
            sampleCount / channels,
            channels,
            frequency,
            false
        );
        newClip.SetData(sampleData, 0);

        // 选择保存路径
        string savePath = EditorUtility.SaveFilePanel(
            "保存裁剪后的WAV文件",
            Application.dataPath,
            $"{clip.name}_{startMs}ms-{endMs}ms.wav",
            "wav"
        );

        if (string.IsNullOrEmpty(savePath)) return;

        // 导出WAV
        try
        {
            SaveWavUtil.Save(savePath, newClip);
            AssetDatabase.Refresh(); // 刷新Project窗口，立即显示文件
            EditorUtility.DisplayDialog(
                "导出成功",
                $"文件已保存至：\n{savePath}",
                "确定"
            );
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("导出失败", $"错误信息：{ex.Message}", "确定");
        }
    }
    #endregion
}
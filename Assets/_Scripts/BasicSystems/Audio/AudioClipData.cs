using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
[System.Serializable]
public class AudioClipData
{
    [Header("基础设置")]
    [SerializeField, Tooltip("随机抽一个片段播放")] private List<AudioClip> _clips = new();// 音频片段列表
    [Range(0, 256), SerializeField] private int _priority = 128;    // 优先级控制
    [SerializeField] private bool _isRandomVolume = false;// 是否启用随机音量
    [Range(0, 1f), SerializeField] private float _volume = 1f;    // 音量控制
    [Range(0, 1f), SerializeField] private float _volumeRandomMin = 0.1f;// 随机音量下限
    [SerializeField] private bool _isRandomPitch = false;// 是否启用随机音调
    [Range(0, 3f), SerializeField] private float _pitch = 1f;   // 音调控制
    [Range(0, 3f), SerializeField] private float _pitchRandomMin = 0.1f;// 随机音调下限
    [Range(0, 1f), SerializeField, Tooltip("空间混合：0为2D音频，1为3D音频")]
    private float _spatialBlend;  // 空间混合

    [Header("可选部分")]
    [SerializeField] private AudioMixerGroup _mixerGroup;
    [SerializeField] private bool _loop = false;

    public AudioClip GetRandomClip() => _clips.Count == 0 ? null : _clips[Random.Range(0, _clips.Count)];
    public int Priority => _priority;
    public float Volume => _isRandomVolume ? Random.Range(_volumeRandomMin, _volume) : _volume;
    public float Pitch => _isRandomPitch ? Random.Range(_pitchRandomMin, _pitch) : _pitch;
    public float SpatialBlend => _spatialBlend;

    public AudioMixerGroup MixerGroup => _mixerGroup;
    public bool Loop => _loop;
}
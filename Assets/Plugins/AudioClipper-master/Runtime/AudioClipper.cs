using UnityEngine;

[CreateAssetMenu(menuName = "NuoYan/AudioClipper")]
public class AudioClipper : ScriptableObject
{
    public AudioClipConfig[] ClipConfigs;
}
[System.Serializable]
public struct AudioClipConfig
{
    public AudioClip ClipSource;
    public float ClipLengthMs; // 音频长度(毫秒)
    public float StartTimeMs; // 开始时间(毫秒)
    public float EndTimeMs; // 结束时间(毫秒)
    public float PreviewPositionMs; // 当前预览位置(毫秒)
    public float PlayStartTime; // 播放开始的系统时间(秒)
    public bool isPlaying;
}
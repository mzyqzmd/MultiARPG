using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
[CreateAssetMenu(fileName = "AudioDatabase", menuName = "MultiARPG/Audio/AudioDatabase")]
public class SO_AudioDatabase : SerializedScriptableObject
{
    [Header("这是两个字典嵌套的AudioDictionary")]
    [DictionaryDrawerSettings(KeyLabel = "音频组名称", ValueLabel = "Key : 音频名称,Value : 音频数据")]
    [OdinSerialize] private readonly Dictionary<string, Dictionary<string, AudioClipData>> _audioDictionary = new();

    [Header("音频发射器设置")]
    [SerializeField] private AudioEmitter _audioEmitterPrefab;         //音频发射器预制体
    [SerializeField] private int _perAudioClipMaxSize = 10;            //每个音频片段的最大实例数量

    [Header("Mixer 引用（按音频类别分配，各音频在 AudioClipData 挂对应 Mixer 的 group）")]
    [SerializeField] private AudioMixer _musicMixer;                  
    [SerializeField] private AudioMixer _sfxMixer;                 
    public AudioEmitter AudioEmitterPrefab => _audioEmitterPrefab;
    public int PerAudioClipMaxSize => _perAudioClipMaxSize;
    public AudioMixer MusicMixer => _musicMixer;
    public AudioMixer SfxMixer => _sfxMixer;
    public AudioClipData GetAudioClipData(string groupName_, string audioName_) =>
        _audioDictionary.TryGetValue(groupName_, out var group) &&
        group.TryGetValue(audioName_, out var data) ? data : null;
}
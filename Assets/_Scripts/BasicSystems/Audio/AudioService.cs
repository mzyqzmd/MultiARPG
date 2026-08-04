using System.Collections.Generic;
using UnityEngine;
public class AudioService : IService
{
    private readonly SO_AudioDatabase _audioDatabase;
    private readonly SettingsSaveService _settingsService;
    private readonly MonoObjectPool<AudioEmitter> _audioEmitterPool;   //本地音频对象池

    private readonly Dictionary<AudioClipData, int> _counts = new();   //音频实例计数
    private readonly List<AudioEmitter> _activeAudioEmitters = new();  //当前活动的本地音频

    public AudioService(SO_AudioDatabase audioDatabase_, Transform gameRootTransform_, SettingsSaveService settingsService_)
    {
        _audioDatabase = audioDatabase_;

        var root = new GameObject("AudioRoot");
        root.transform.SetParent(gameRootTransform_);
        _audioEmitterPool = new(_audioDatabase.AudioEmitterPrefab, root.transform);

        _settingsService = settingsService_;

        // 构造即应用一次当前音量，并订阅设置变更（滑块实时预览/落盘都会触发）
        ApplyVolumes();
        _settingsService.OnChanged += ApplyVolumes;
    }

    #region 音量应用（两个独立 Mixer 各自 SetFloat 其 exposed 参数，线性→dB）
    /// <summary>把设置里的线性音量推给两个 Mixer 的 exposed 参数；各音频路由由其挂的 group 决定，此处不干预。</summary>
    private void ApplyVolumes()
    {
        if (_audioDatabase.MusicMixer)
            _audioDatabase.MusicMixer.SetFloat("MusicVolume", LinearToDb(_settingsService.SettingsData.musicVolume));
        if (_audioDatabase.SfxMixer)
            _audioDatabase.SfxMixer.SetFloat("SFXVolume", LinearToDb(_settingsService.SettingsData.sfxVolume));
    }

    // 线性音量(0~1)转分贝：0 时给 -80dB（静音），否则 20*log10(v)
    private static float LinearToDb(float value_) =>
        value_ <= 0.0001f ? -80f : Mathf.Log10(value_) * 20f;
    #endregion

    #region 播放指定音频
    public AudioEmitter PlayAudio(string groupName_, string audioName_, Vector3 followTargetPosition_ = default)
    {
        AudioClipData data = _audioDatabase.GetAudioClipData(groupName_, audioName_);
        if (data == null) return null;
        if (_counts.ContainsKey(data) && _counts[data] >= _audioDatabase.PerAudioClipMaxSize) return null;

        //播放音频
        AudioEmitter emitter = _audioEmitterPool.Get();
        _activeAudioEmitters.Add(emitter);
        emitter.InitializeAudioEmitter(data, followTargetPosition_);
        emitter.OnAudioFinished.AddListener(HandleAudioFinished);
        emitter.Play();

        // 更新实例计数
        if (!_counts.ContainsKey(data)) _counts[data] = 0;
        _counts[data]++;

        return emitter;
    }

    private void HandleAudioFinished(AudioEmitter emitter_)
    {
        emitter_.OnAudioFinished.RemoveAllListeners();
        _counts[emitter_.AudioClipData]--;
        _activeAudioEmitters.Remove(emitter_);
        _audioEmitterPool.Release(emitter_);
    }
    #endregion

    #region 停止播放指定音频
    public void StopAudio(string groupName_, string audioName_)
    {
        var data = _audioDatabase.GetAudioClipData(groupName_, audioName_);
        if (data == null) return;
        for (int i = _activeAudioEmitters.Count - 1; i >= 0; i--)
        {
            if (_activeAudioEmitters[i].AudioClipData == data)
                _activeAudioEmitters[i].Stop();
        }
    }
    #endregion

    #region 停止播放所有音频
    public void StopAllAudio()
    {
        // 创建一个新的列表来缓存当前所有活动的发射器，避免在迭代过程中修改原列表
        List<AudioEmitter> emittersToStop = new(_activeAudioEmitters);
        foreach (var emitter in emittersToStop)
            emitter.Stop();
    }
    #endregion
}
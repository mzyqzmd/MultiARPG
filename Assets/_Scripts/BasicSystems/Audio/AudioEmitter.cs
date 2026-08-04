using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
public class AudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    public AudioClipData AudioClipData { get; private set; }
    public UnityEvent<AudioEmitter> OnAudioFinished { get; private set; } = new();

    private Vector3 _followTargetPosition;
    private Coroutine _playingCoroutine;

    private void Update()
    {
        // 如果正在播放，持续更新位置以跟随目标
        if (_audioSource.isPlaying)
            transform.position = _followTargetPosition;
    }

    public void InitializeAudioEmitter(AudioClipData data_, Vector3 followTargetPosition_ = default)
    {
        AudioClipData = data_;
        _followTargetPosition = followTargetPosition_;
        transform.position = _followTargetPosition;
        _audioSource.clip = data_.GetRandomClip();
        _audioSource.priority = data_.Priority;
        _audioSource.volume = data_.Volume;
        _audioSource.pitch = data_.Pitch;
        _audioSource.spatialBlend = data_.SpatialBlend;
        _audioSource.outputAudioMixerGroup = data_.MixerGroup;
        _audioSource.loop = data_.Loop;
    }

    #region 播放音频
    public void Play()
    {
        // 如果正在播放，先中断之前的协程
        if (_playingCoroutine != null)
            StopCoroutine(_playingCoroutine);

        _audioSource.Play();
        _playingCoroutine = StartCoroutine(WaitForAudioToEnd());
    }
    #endregion

    #region 等待音频播放完毕
    private IEnumerator WaitForAudioToEnd()
    {
        yield return new WaitWhile(() => _audioSource.isPlaying);
        Stop();
    }
    #endregion

    #region 停止播放音频
    public void Stop()
    {
        // 停止音频源播放
        _audioSource.Stop();

        // 停止协程并清空引用
        if (_playingCoroutine != null)
        {
            StopCoroutine(_playingCoroutine);
            _playingCoroutine = null;
        }
        OnAudioFinished?.Invoke(this);
    }
    #endregion
}

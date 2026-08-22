using UnityEngine;

/// <summary>场景区域 BGM 触发器：本地玩家（owner）进入碰撞体范围播放指定 BGM，离开停止。其他端各自独立触发。</summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class AudioPlayBox : MonoBehaviour
{
    [SerializeField] private string _audioGroup = "Music";
    [SerializeField] private string _audioName;

    private AudioService _audioService;

    #region 生命周期
    private void Awake()
    {
        _audioService = Services.Get<AudioService>();
    }
    #endregion

    #region 触发器（仅本地 owner）
    private void OnTriggerEnter(Collider other_)
    {
        var player = other_.GetComponentInParent<Player>();
        if (player == null || !player.IsOwner) return;
        _audioService.PlayAudio(_audioGroup, _audioName);
    }

    private void OnTriggerExit(Collider other_)
    {
        var player = other_.GetComponentInParent<Player>();
        if (player == null || !player.IsOwner) return;
        _audioService.StopAudio(_audioGroup, _audioName);
    }
    #endregion

}

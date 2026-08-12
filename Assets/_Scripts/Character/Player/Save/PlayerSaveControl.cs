using UnityEngine;

/// <summary>
/// 玩家存档能力（纯本地）：累计时长、组装本机 PlayerSaveData / WorldSaveData、从存档还原本机 Transform。
/// 不持有网络字段、不写文件——写盘由 Host 侧 SaveService 单文件落盘，本类只负责"数据组装/还原"。
/// </summary>
public class PlayerSaveControl : MonoBehaviour
{
    [Header("玩家引用")]
    [SerializeField] private Player _player;

    private float _secondsPlayed;

    private void Update()
    {
        //游玩时长累计（仅世界场景）
        if (_player.SceneControlService.CurrentActiveScene.name == SceneControlService.SceneWorld)
            _secondsPlayed += Time.deltaTime;
    }

    #region 组装存档数据
    /// <summary>组装本机玩家存档：绑定传入 playerId + 当前坐标/朝向 + 当前生命值。</summary>
    public PlayerSaveData BuildPlayerSaveData(string playerId_) => new()
    {
        playerId = playerId_,
        characterName = "Player",//TODO:目前算是占位符
        currentHealth = _player.StatsControl.CurrentHealth,
        position = transform.position,
        rotation = transform.rotation,
    };

    /// <summary>组装世界状态（仅 Host 调用）：当前激活场景名 + 世界累计时长。</summary>
    public WorldSaveData BuildWorldSaveData() =>
        new()
        {
            activeSceneName = _player.SceneControlService.CurrentActiveScene.name,
            secondsPlayed = _secondsPlayed,
        };
    #endregion

    #region 从存档还原本机
    /// <summary>从单个玩家存档还原本机坐标/朝向。</summary>
    public void RestoreFromPlayer(PlayerSaveData data_)
    {
        if (data_ == null) return;
        transform.SetPositionAndRotation(data_.position, data_.rotation);
    }

    /// <summary>Host 重入世界时续算世界级累计时长（玩家数据已不含时长，时长归 World）。</summary>
    public void RestoreWorldTime(float secondsPlayed_) => _secondsPlayed = secondsPlayed_;
    #endregion
}

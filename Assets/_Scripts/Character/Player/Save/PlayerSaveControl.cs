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
    private Vector3 _respawnPosition;
    private Quaternion _respawnRotation;
    private bool _respawnPointRecorded;

    private void Update()
    {
        // 非世界场景直接跳过：既不累计时长，也不记录出生点（玩家在 MainMenu 就已 spawn，坐标无效）
        if (_player.SceneControlService.CurrentActiveScene.name != SceneControlService.SceneWorld)
            return;
        _secondsPlayed += Time.deltaTime;
        // 首次进入世界场景记录出生点（默认复活点，之后被存档点覆盖）
        if (!_respawnPointRecorded)
        {
            _respawnPosition = transform.position;
            _respawnRotation = transform.rotation;
            _respawnPointRecorded = true;
        }
    }

    #region 组装存档数据
    /// <summary>组装本机玩家存档：绑定传入 playerId + 当前坐标/朝向 + 当前生命值。</summary>
    public PlayerSaveData BuildPlayerSaveData(string playerId_) => new()
    {
        playerId = playerId_,
        characterName = $"Player:{playerId_}",
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

    #region 复活点
    /// <summary>登记复活点（存档点触碰时调用）：记录存档点物体自身的位置与朝向。仅存本地内存，不进存档、不跨端同步。</summary>
    public void SetRespawnPoint(Transform point_) => SetRespawnPoint(point_.position, point_.rotation);
    /// <summary>登记复活点（指定坐标/朝向版）。</summary>
    public void SetRespawnPoint(Vector3 position_, Quaternion rotation_)
    {
        _respawnPosition = position_;
        _respawnRotation = rotation_;
        _respawnPointRecorded = true;
    }
    /// <summary>复活点坐标（未登记过则返回出生点）。</summary>
    public Vector3 RespawnPosition => _respawnPosition;
    /// <summary>复活点朝向（未登记过则返回出生点朝向）。</summary>
    public Quaternion RespawnRotation => _respawnRotation;
    #endregion
}

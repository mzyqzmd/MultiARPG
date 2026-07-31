using Unity.Netcode;

/// <summary>
/// 角色网络同步状态组件（独立 NetworkBehaviour，与 Character 同挂一个 GameObject）：
/// 承载需要共享给其他玩家的运行时状态。当前仅死亡布尔，由 server 权威置位，经 NetworkVariable 广播所有客户端。
/// 不持有本地 stats 引用、不反向依赖 Character，符合网络控制组件铁律。
/// 死亡不入存档，复活/重连由 server 在 OnNetworkSpawn 复位为 false。
/// </summary>
public class CharacterNetworkStatsControl : NetworkBehaviour
{
    // server 权威的死亡状态，所有客户端自动同步
    private readonly NetworkVariable<bool> _isDead = new();

    /// <summary>死亡状态翻转事件（old, new），供各端订阅接入死亡表现（含其他玩家）。</summary>
    public event System.Action<bool, bool> OnDeathStateChanged;

    /// <summary>当前是否死亡（纯读取）。</summary>
    public bool IsDead => _isDead.Value;

    #region 死亡上报与置位
    /// <summary>owner 端本地血量归零时调用：server（Host）直置，否则经 ServerRpc 上报。</summary>
    public void ReportDeath()
    {
        if (IsServer) SetDead();
        else ReportDeathServerRpc();
    }

    /// <summary>server 权威置死亡（复活/重连复位用 SetAlive）。</summary>
    public void SetDead()
    {
        if (!IsServer) return;
        _isDead.Value = true;
    }

    /// <summary>server 权威复活：复位死亡状态（死亡不存档，重入/复活后状态干净）。</summary>
    public void SetAlive()
    {
        if (!IsServer) return;
        _isDead.Value = false;
    }

    [ServerRpc]
    private void ReportDeathServerRpc() => SetDead();
    #endregion

    #region 网络生命周期
    public override void OnNetworkSpawn()
    {
        _isDead.OnValueChanged += OnIsDeadChanged;
        // 复活/重连复位：死亡不存档，server 保证新一局/重连后初始为存活
        if (IsServer) _isDead.Value = false;
    }

    public override void OnNetworkDespawn() => _isDead.OnValueChanged -= OnIsDeadChanged;
    #endregion

    private void OnIsDeadChanged(bool old_, bool new_) => OnDeathStateChanged?.Invoke(old_, new_);
}

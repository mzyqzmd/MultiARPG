using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Boss 僵尸网络同步层（独立 NetworkBehaviour，与 BossZombie 同挂一个 GameObject）。
/// 仅持有跨端需可读的数值：当前血量 _currentHP（Host 权威写入）。
/// 阶段与死亡走 Animator 参数经 NetworkAnimator 同步，不在此开额外 NetworkVariable。
/// OnValueChanged 驱动血条 UI 与 HP 阈值通知（交由领域类编排阶段切换），自身不承载编排逻辑。
/// </summary>
public class BossZombieNetworkSync : NetworkBehaviour
{
    [SerializeField] private BossZombie _bossZombie;

    private NetworkVariable<int> _currentHP = new NetworkVariable<int>();

    /// <summary>当前血量变化（含客户端同步到的值），供血条 UI 订阅。</summary>
    public event Action<int, int> OnHpChanged;
    /// <summary>HP 过 50% 阈值（仅在 Host 端、Phase1 时触发一次），通知领域类发起阶段切换。</summary>
    public event Action OnPhaseThresholdCrossed;

    private int _maxHP;
    private bool _thresholdFired;

    public int CurrentHP => _currentHP.Value;
    public int MaxHP => _maxHP;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _maxHP = _bossZombie.BossZombieStatsControl.MaxHealth;
            _currentHP.Value = _maxHP;
            _thresholdFired = false;
        }
        _currentHP.OnValueChanged += OnHpChangedHandler;
        if (IsClient && !IsServer)
            OnHpChanged?.Invoke(_currentHP.Value, _currentHP.Value);
    }

    public override void OnNetworkDespawn()
    {
        _currentHP.OnValueChanged -= OnHpChangedHandler;
        base.OnNetworkDespawn();
    }

    /// <summary>Host 端扣血入口：由 BossZombie 在本地扣血后同步到 NV。</summary>
    public void SetHP(int value_)
    {
        if (!IsServer) return;
        _currentHP.Value = Mathf.Clamp(value_, 0, _maxHP);
    }

    private void OnHpChangedHandler(int oldValue_, int newValue_)
    {
        OnHpChanged?.Invoke(oldValue_, newValue_);
        if (IsServer && !_thresholdFired && _maxHP > 0 && newValue_ <= _maxHP * 0.5f
            && _bossZombie.Phase == EBossZombiePhase.Phase1)
        {
            _thresholdFired = true;
            OnPhaseThresholdCrossed?.Invoke();
        }
    }
}

using UnityEngine;

/// <summary>
/// 游戏内 UI Presenter（MVP 的 Presenter，普通 C# 类）：集中持有模型与各子视图引用，统一驱动每帧平滑刷新。
/// 耐力条/血条均改为事件驱动：数值变化事件只更新目标比例，Tick 负责 Lerp 平滑逼近，不再每帧主动读取模型/网络比例。
/// </summary>
public class InGameUIPresenter
{
    private readonly Player _player;
    private readonly UI_StaminaBar _staminaBar;
    private readonly UI_HealthBar _healthBar;
    private readonly UI_WeaponQuickPanel _weaponQuickPanel;

    private float _staminaRatio;
    private float _targetStaminaRatio;
    private float _healthRatio;
    private float _targetHealthRatio;

    public InGameUIPresenter(UI_InGameUI view_, Player player_)
    {
        _player = player_;
        _staminaBar = view_.StaminaBar;
        _healthBar = view_.HealthBar;
        _weaponQuickPanel= view_.WeaponQuickPanel;

        _player.StatsControl.OnStaminaChanged += OnStaminaChanged;
        _player.StatsControl.OnCurrentHealthChanged += OnHealthChanged;
        _weaponQuickPanel.Initialize(_player.EquipmentControl);
        // 除零守卫：构造时 MaxHealth 可能尚未初始化，默认满值
        _staminaRatio = _targetStaminaRatio = _player.StatsControl.MaxStamina > 0f ? _player.StatsControl.StaminaRatio : 1f;
        _healthRatio = _targetHealthRatio = _player.StatsControl.MaxHealth > 0f ? _player.StatsControl.HealthRatio : 1f;
    }

    public void Tick(float deltaTime_)
    {
        _staminaRatio = Mathf.Lerp(_staminaRatio, _targetStaminaRatio, deltaTime_ * 12f);
        _staminaBar.SetFillRatio(_staminaRatio);
        _healthRatio = Mathf.Lerp(_healthRatio, _targetHealthRatio, deltaTime_ * 12f);
        _healthBar.SetFillRatio(_healthRatio);
    }

    private void OnStaminaChanged(int _, int newStamina_)
    {
        if (_player.StatsControl.MaxStamina > 0f) _targetStaminaRatio = (float)newStamina_ / _player.StatsControl.MaxStamina;
    }

    private void OnHealthChanged(int _, int newHealth_)
    {
        if (_player.StatsControl.MaxHealth > 0f) _targetHealthRatio = (float)newHealth_ / _player.StatsControl.MaxHealth;
    }

    #region 生命周期
    /// <summary>UI 解绑销毁时调用：退订两个事件，避免悬空回调与内存泄漏。</summary>
    public void Dispose()
    {
        _player.StatsControl.OnStaminaChanged -= OnStaminaChanged;
        _player.StatsControl.OnCurrentHealthChanged -= OnHealthChanged;
    }
    #endregion
}

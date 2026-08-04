using UnityEngine;

/// <summary>
/// 游戏内 UI 容器（MVP 的 View）：统一承载所有世界场景下的游戏 HUD（当前含耐力条，后续可扩展血条、连击等）。
/// 纯展示容器，自身不含业务逻辑；每帧 Update 驱动 InGameUIPresenter.Tick，由 Presenter 分发刷新各子视图。
/// </summary>
public class UI_InGameUI : UI_BaseView
{
    [SerializeField] private UI_StaminaBar _staminaBar;
    [SerializeField] private UI_HealthBar _healthBar;

    private InGameUIPresenter _presenter;

    public UI_StaminaBar StaminaBar => _staminaBar;
    public UI_HealthBar HealthBar => _healthBar;

    public void Initialize(PlayerStatsControl statsControl_) =>
        _presenter = new InGameUIPresenter(statsControl_, this);

    private void Update() => _presenter?.Tick(Time.deltaTime);

    private void OnDestroy() => _presenter?.Dispose();
}
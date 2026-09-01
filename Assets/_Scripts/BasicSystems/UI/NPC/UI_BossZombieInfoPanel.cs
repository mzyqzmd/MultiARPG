using UnityEngine;

/// <summary>
/// Boss 僵尸信息面板（MVP 的 View）：纯展示，只暴露血条比例设置入口，不含任何逻辑。
/// 全部逻辑由 BossZombieInfoPanelPresenter 持有；驱动源经 Tick 由 BossZombie 转发
/// （本视图 Hide 走 SetActive(false)，自身 Update 会停止，无法自检玩家重新进场）。
/// </summary>
public class UI_BossZombieInfoPanel : UI_BaseView
{
    [SerializeField] private UI_HealthBar _healthBar;

    private BossZombieInfoPanelPresenter _presenter;

    public void Initialize(BossZombie bossZombie_) =>
        _presenter = new BossZombieInfoPanelPresenter(this, bossZombie_);

    public void Tick(float deltaTime_) => _presenter?.Tick(deltaTime_);

    public void SetFillRatio(float ratio_) => _healthBar.SetFillRatio(ratio_);

    private void OnDestroy() => _presenter?.Dispose();
}

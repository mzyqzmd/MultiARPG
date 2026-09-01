using UnityEngine;

/// <summary>
/// Boss 信息面板 Presenter（MVP 的 Presenter，普通 C# 类）：承担"是否显示"与"显示多少"两件事。
/// 血量只读 BossZombie 的跨端广播镜像（客户端本地 StatsControl 恒满血，不可读）；
/// 驱动源由 Boss 侧转发——面板 Hide 会停掉自身 Update，无法自检玩家重新进场。
/// </summary>
public class BossZombieInfoPanelPresenter
{
    private const float PollInterval = 0.2f;
    private const float LerpSpeed = 12f;

    private readonly UI_BossZombieInfoPanel _view;
    private readonly BossZombie _boss;
    private readonly UIService _uiService;

    private float _displayRatio;
    private float _targetRatio;
    private float _distanceTimer;

    public BossZombieInfoPanelPresenter(UI_BossZombieInfoPanel view_, BossZombie bossZombie_)
    {
        _view = view_;
        _boss = bossZombie_;
        _uiService = Services.Get<UIService>();

        _boss.OnSyncedHealthChanged += OnHealthChanged;
        // 构造即按当前同步值初始化，避免首帧闪现满血条；late-join 由 NV 初值保证读到真实血量。
        _displayRatio = _targetRatio = ComputeRatio(_boss.SyncedHealth);
        RefreshVisibility();
    }

    #region 每帧驱动（由 BossZombie.Update 转发，面板隐藏时同样持续推进）
    public void Tick(float deltaTime_)
    {
        _distanceTimer += deltaTime_;
        if (_distanceTimer >= PollInterval)
        {
            _distanceTimer = 0f;
            RefreshVisibility();
        }

        _displayRatio = Mathf.Lerp(_displayRatio, _targetRatio, deltaTime_ * LerpSpeed);
        _view.SetFillRatio(_displayRatio);
    }
    #endregion

    #region 生命周期
    public void Dispose() => _boss.OnSyncedHealthChanged -= OnHealthChanged;
    #endregion

    /// <summary>按本地玩家与 Boss 的距离决定显隐：本地玩家未绑定或超出激活半径则收起。</summary>
    private void RefreshVisibility()
    {
        var player = _uiService.LocalPlayer;
        if (player == null)
        {
            _view.Hide();
            return;
        }

        float sqrDistance = (player.transform.position - _boss.transform.position).sqrMagnitude;
        float radius = _boss.ActivationRadius;
        if (sqrDistance <= radius * radius)
            _view.Show();
        else
            _view.Hide();
    }

    private void OnHealthChanged(int _) => _targetRatio = ComputeRatio(_boss.SyncedHealth);

    private float ComputeRatio(int health_)
    {
        int max = _boss.StatsControl.MaxHealth;
        return max > 0 ? (float)health_ / max : 0f;
    }
}

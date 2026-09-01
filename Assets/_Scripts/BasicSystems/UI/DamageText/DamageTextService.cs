using UnityEngine;

/// <summary>
/// 伤害飘字服务（IService 普通类）：持有世界空间飘字对象池，按命中点生成伤害数字。
/// 纯本地表现，零网络依赖——仅攻击者本机在命中瞬间调用，与飙血 VFX / 受击音效同属「先本地再通知」反馈。
/// 预制体经 SO_UIDatabase（key "DamageText"）取；取预制体、查表、效果类型判定任何一步失败都只记录后返回，
/// 绝不阻断既有伤害结算 RPC（飘字是纯旁支表现）。
/// </summary>
public class DamageTextService : IService
{
    private const string DamageTextKey = "DamageText";
    private const int DefaultCapacity = 8;
    private const int MaxSize = 16;

    private readonly MonoObjectPool<UI_DamageText> _pool;
    private readonly EffectService _effectService;

    public DamageTextService(UIService uiService_, EffectService effectService_, Transform gameRoot_)
    {
        _effectService = effectService_;

        var prefab = uiService_.GetUiPrefab(DamageTextKey) as UI_DamageText;
        if (prefab == null)
        {
            Debug.LogError($"[DamageTextService] SO_UIDatabase 未注册 key \"{DamageTextKey}\"，或其预制体未挂 UI_DamageText。伤害飘字已禁用（不影响伤害结算）。");
            return;
        }

        var root = new GameObject("DamageTextRoot");
        root.transform.SetParent(gameRoot_);
        _pool = new MonoObjectPool<UI_DamageText>(prefab, root.transform,
            onRelease_: text_ => text_.ClearForReuse(), defaultCapacity_: DefaultCapacity, maxSize_: MaxSize);
    }

    #region 按效果 id 与倍率预结算总伤害后弹出
    /// <summary>查表取 DamageEffect，用与目标端扣血完全一致的 Compute 口径（物理 / 魔法各自乘倍率、各自四舍五入）求和后弹出。</summary>
    public void SpawnByEffect(string effectId_, float multiplier_, Vector3 worldPosition_)
    {
        if (_pool == null) return;
        // 类型不匹配（如武器配了治疗 effect）时静默跳过：GetEffect 已对未注册 id 报错，此处不重复打日志。
        var damageEffect = _effectService.GetEffect(effectId_) as DamageEffect;
        if (damageEffect == null) return;
        (int physical, int magic) = damageEffect.Compute(multiplier_);
        Spawn(physical + magic, worldPosition_);
    }
    #endregion

    #region 在世界坐标弹出指定伤害数字
    /// <summary>取池实例并播放飘字动画；未注册预制体时静默跳过。</summary>
    public void Spawn(int damage_, Vector3 worldPosition_)
    {
        if (_pool == null) return;
        var damageText = _pool.Get();
        damageText.BindService(this);
        damageText.Initialize(damage_, worldPosition_);
    }
    #endregion

    #region 回收飘字实例
    /// <summary>回收飘字实例（由 UI_DamageText 动画结束时回调）。</summary>
    public void Release(UI_DamageText text_) => _pool?.Release(text_);
    #endregion
}

using UnityEngine;

/// <summary>
/// 效果服务（IService 普通类）：构造注入 SO_EffectDatabase，对外提供 GetEffect(id) 查表 + 实例拷贝。
/// 拷贝用 MemberwiseClone（BaseEffect 子类字段均为值类型/string/SerializedField 值类型，无引用类型字段，
/// 故浅拷贝语义上等价于深拷贝，可防止 Timed 效果如 DamageMultiplierEffect._originalMultiplier 等运行时状态被多实例并发污染）。
/// 未注册 id 返回 null 并 LogError。
/// </summary>
public class EffectService : IService
{
    private readonly SO_EffectDatabase _database;

    public EffectService(SO_EffectDatabase database_) => _database = database_;

    #region 查表与拷贝
    /// <summary>按 id 查表并返回独立实例拷贝；未注册返回 null 并 LogError。</summary>
    public BaseEffect GetEffect(string id_)
    {
        var prototype = _database.GetPrototype(id_);
        if (prototype == null)
        {
            Debug.LogError($"[EffectService] 未注册的效果 id: {id_}");
            return null;
        }
        return prototype.Clone();
    }
    #endregion
}

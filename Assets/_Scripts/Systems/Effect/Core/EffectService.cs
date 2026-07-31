/// <summary>效果查询服务（IService）：按 EffectId 从数据库取效果资产，供施加入口（ServerRpc）使用。</summary>
public class EffectService : IService
{
    private readonly SO_EffectDatabase _database;
    public EffectService(SO_EffectDatabase database_) => _database = database_;

    public SO_BaseEffect GetEffect(string id_) => _database.GetEffect(id_);
}

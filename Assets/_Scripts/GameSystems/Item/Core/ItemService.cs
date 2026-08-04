using UnityEngine;


/// <summary>物品查询服务（IService）：按物品 id 从数据库取物品预制体，供拔刀/收刀表现层使用。</summary>
public class ItemService : IService
{
    private readonly SO_ItemDatabase _database;
    public ItemService(SO_ItemDatabase database_) => _database = database_;

    public BaseItem GetItemPrefab(string id_) => _database.GetItemPrefab(id_);
}

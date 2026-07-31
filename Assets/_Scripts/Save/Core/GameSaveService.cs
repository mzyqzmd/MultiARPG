using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 存档服务（IService）：单文件架构的底层编排入口。一个槽 = 一个 .save 文件，
/// Host 持有的 <see cref="GameSaveData"/> 工作副本（_currentData）是该世界的唯一真源
/// （含 World + 全部玩家 Players）。客人不写文件，只经 RPC 把自身 PlayerSaveData 上报给 Host 合并。
/// 仅认数据与本地文件 IO（SaveFileDataWriter），不耦合 Player / UI / Network / Scene。
/// </summary>
public class GameSaveService : IService
{
    private const int _maxSlots = 10;

    private readonly SaveFileDataWriter _dataWriter;
    private readonly string _saveDirectory;

    private string _currentSlotId;        // 当前槽 Id
    private GameSaveData _currentData;     // 当前槽工作副本（单一真源，Host 恢复自身/合并客人均基于它）

    /// <summary>当前槽 Id（Host 世界 id）。</summary>
    public string CurrentSlotId => _currentSlotId;

    /// <summary>构造注入文件 IO + 存档目录（persistentDataPath/Saves/&lt;suffix&gt;，单机双开按实例隔离）。</summary>
    public GameSaveService(SaveFileDataWriter dataWriter_, string saveDirectory_)
    {
        _dataWriter = dataWriter_;
        _saveDirectory = saveDirectory_;
        Directory.CreateDirectory(_saveDirectory);
    }

    private string SlotPath(string slotId_) => Path.Combine(_saveDirectory, slotId_ + ".save");

    #region 列出所有存档槽（最多 MaxSlots）
    /// <summary>列出所有存档槽（slotId + 元数据），最多返回 MaxSlots 个。</summary>
    public List<(string slotId, GameSaveData meta)> ListSlots()
    {
        var result = new List<(string, GameSaveData)>();
        foreach (var file in Directory.GetFiles(_saveDirectory, "*.save"))
        {
            var meta = _dataWriter.Read<GameSaveData>(file);
            if (meta != null)
                result.Add((Path.GetFileNameWithoutExtension(file), meta));
            if (result.Count >= _maxSlots) break;
        }
        return result;
    }
    #endregion

    #region 新建存档槽
    /// <summary>生成新槽（slot_{Guid}），写空世界+空玩家列表并设为当前槽+待恢复；满 MaxSlots 返回 false。</summary>
    public bool CreateNewSlot()
    {
        if (ListSlots().Count >= _maxSlots) return false;
        string slotId = "slot_" + Guid.NewGuid().ToString("N");
        var data = new GameSaveData { World = null, Players = new List<PlayerSaveData>() };
        _dataWriter.Write(SlotPath(slotId), data);
        _currentSlotId = slotId;
        _currentData = data;
        return true;
    }
    #endregion

    #region 选择已有存档槽
    /// <summary>读取指定槽为工作副本并设为当前槽+待恢复。</summary>
    public void SelectSlot(string slotId_)
    {
        string path = SlotPath(slotId_);
        if (!_dataWriter.IsSaveFileExists(path)) return;
        _currentSlotId = slotId_;
        _currentData = _dataWriter.Read<GameSaveData>(path) ?? new GameSaveData();
        _currentData.Players ??= new List<PlayerSaveData>();
    }
    #endregion

    #region 删除存档槽
    /// <summary>删除指定槽；若正为当前槽则清空当前标记与工作副本。</summary>
    public void DeleteSlot(string slotId_)
    {
        string path = SlotPath(slotId_);
        if (_dataWriter.IsSaveFileExists(path)) _dataWriter.DeleteSaveFile(path);
        if (_currentSlotId == slotId_)
        {
            _currentSlotId = null;
            _currentData = null;
        }
    }
    #endregion

    #region 玩家列表增改查（Host 单一真源，按 playerId）
    /// <summary>按 playerId 取玩家存档；无则 null。</summary>
    public PlayerSaveData GetPlayer(string playerId_) =>
        _currentData?.Players.Find(p_ => p_.playerId == playerId_);

    /// <summary>插入或更新玩家存档（按 playerId 去重）。</summary>
    public void UpsertPlayer(PlayerSaveData player_)
    {
        if (_currentData == null || player_ == null) return;
        int index = _currentData.Players.FindIndex(p_ => p_.playerId == player_.playerId);
        if (index >= 0) _currentData.Players[index] = player_;
        else _currentData.Players.Add(player_);
    }

    /// <summary>移除指定玩家存档。</summary>
    public void RemovePlayer(string playerId_) =>
        _currentData?.Players.RemoveAll(p_ => p_.playerId == playerId_);

    /// <summary>只读遍历当前世界全部玩家存档。</summary>
    public IReadOnlyList<PlayerSaveData> ListPlayers() =>
        _currentData?.Players ?? new List<PlayerSaveData>();

    /// <summary>设置世界状态（仅 Host 调用）。</summary>
    public void SetWorld(WorldSaveData world_)
    {
        if (_currentData != null) _currentData.World = world_;
    }

    /// <summary>读取当前世界状态（仅 Host 持有；客人返回 null）。</summary>
    public WorldSaveData GetWorld() => _currentData?.World;
    #endregion

    #region 写回当前槽（仅 Host：单文件落盘工作副本）
    /// <summary>将工作副本写入当前槽文件；无当前槽（客人/未选槽）则短路。</summary>
    public void SaveCurrentSlot()
    {
        if (string.IsNullOrEmpty(_currentSlotId) || _currentData == null) return;
        _dataWriter.Write(SlotPath(_currentSlotId), _currentData);
    }
    #endregion
}

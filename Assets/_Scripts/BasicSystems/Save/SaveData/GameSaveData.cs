using System;
using System.Collections.Generic;

/// <summary>
/// 一个存档槽 = 一个文件。单一真源由 Host 持有：World 为世界共享状态（仅 Host 写），
/// Players 为该世界内全部玩家的存档列表（Host 自己 + 各客人经上报合并），按 playerId 唯一标识。
/// </summary>
[Serializable]
public class GameSaveData
{
    //WorldState (Host only)
    public WorldSaveData World;

    //All players in this world (keyed by playerId inside each element)
    public List<PlayerSaveData> Players = new();
}

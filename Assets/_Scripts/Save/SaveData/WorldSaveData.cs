using System;

/// <summary>
/// 世界状态存档数据（仅由 Host 端负责写入，Client 端不存储）。
/// 当前仅含当前激活场景名，后续可扩展世界实体/任务/时间等共享状态。
/// </summary>
[Serializable]
public class WorldSaveData
{
    //ActiveSceneName
    public string activeSceneName;

    //WorldCumulativePlayTime（一个世界一份，仅 Host 写；重入世界时续算）
    public float secondsPlayed;
}

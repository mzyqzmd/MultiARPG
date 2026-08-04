using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 单个玩家的存档数据。playerId 为该玩家本地持久 GUID（跨会话稳定，见 LocalPlayerId），
/// 是 Host 单文件中 Players 列表的唯一键。实现 INetworkSerializable 以便作为整包在 RPC 中上报/下发。
/// </summary>
[Serializable]
public class PlayerSaveData : INetworkSerializable
{
    //Identity（本地持久 GUID，跨会话稳定，作 Players 列表唯一键）
    public string playerId;

    //CharacterName
    public string characterName;

    //CurrentHealth
    public int currentHealth;

    //WorldCoordinates
    public Vector3 position;
    public Quaternion rotation;

    /// <summary>NGO 网络序列化：string 走 FixedString（NGO 不原生支持 string），其余原生类型直接读写。</summary>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer_) where T : IReaderWriter
    {
        FixedString128Bytes id = playerId ?? string.Empty;
        FixedString64Bytes name = characterName ?? string.Empty;
        serializer_.SerializeValue(ref id);
        serializer_.SerializeValue(ref name);
        serializer_.SerializeValue(ref currentHealth);
        serializer_.SerializeValue(ref position);
        serializer_.SerializeValue(ref rotation);
        if (serializer_.IsReader)
        {
            playerId = id.ToString();
            characterName = name.ToString();
        }
    }
}

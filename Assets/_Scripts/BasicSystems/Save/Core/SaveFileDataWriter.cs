using Sirenix.Serialization;
using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 本地文件 IO：按完整文件路径进行无状态泛型读写/删/存在判断，全项目复用（存档 + 全局设置）。
/// 序列化采用 Sirenix.OdinSerializer（DataFormat.JSON），原生支持 Vector3/Quaternion/Dictionary/List。
/// </summary>
public class SaveFileDataWriter
{
    // 判断文件是否存在
    public bool IsSaveFileExists(string fullPath_) =>
        File.Exists(fullPath_);

    // 删除文件
    public void DeleteSaveFile(string fullPath_) =>
        File.Delete(fullPath_);

    #region 将数据写入完整路径
    public void Write<T>(string fullPath_, T data_) where T : class
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath_));
            string dataToStore = Encoding.UTF8.GetString(
                SerializationUtility.SerializeValue(data_, DataFormat.JSON));
            using FileStream stream = new(fullPath_, FileMode.Create);
            using StreamWriter writer = new(stream);
            writer.Write(dataToStore);
        }
        catch (Exception e)
        {
            Debug.LogError($"存档失败,未能保存至{fullPath_}\n{e.Message}");
        }
    }
    #endregion

    #region 从完整路径读取数据
    public T Read<T>(string fullPath_) where T : class
    {
        if (!IsSaveFileExists(fullPath_)) return null;
        try
        {
            using FileStream stream = new(fullPath_, FileMode.Open);
            using StreamReader reader = new(stream);
            string dataToLoad = reader.ReadToEnd();
            return SerializationUtility.DeserializeValue<T>(
                Encoding.UTF8.GetBytes(dataToLoad), DataFormat.JSON);
        }
        catch
        {
            Debug.LogError("读取失败,未能读取文件");
            return null;
        }
    }
    #endregion
}

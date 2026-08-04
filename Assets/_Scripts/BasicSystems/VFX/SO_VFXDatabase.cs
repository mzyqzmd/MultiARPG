using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VFXDatabase", menuName = "MultiARPG/VFX/VFXDatabase")]
public class SO_VFXDatabase : SerializedScriptableObject
{
    [Header("特效名 → VFXController（直接拖入挂该脚本的预制体）")]
    [DictionaryDrawerSettings(KeyLabel = "特效名", ValueLabel = "VFXController")]
    [OdinSerialize] private readonly Dictionary<string, VFXController> _vfxPrefabs = new();

    [Tooltip("每类特效对象池容量上限，超出回收的实例直接销毁")]
    [SerializeField] private int _maxPoolPerVfx = 16;

    /// <summary>每类特效对象池容量上限。</summary>
    public int MaxPoolPerVfx => _maxPoolPerVfx;

    public VFXController GetController(string vfxName_) =>
        _vfxPrefabs.TryGetValue(vfxName_, out var controller) ? controller : null;
}

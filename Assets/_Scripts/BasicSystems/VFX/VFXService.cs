using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 视觉特效服务（纯本地，零网络依赖）。按特效名取预制体实例化并播放，
/// 支持按名字手动停止全部同名 VFX。实例复用走 MonoObjectPool，按名分桶。
/// </summary>
public class VFXService : IService
{
    private readonly SO_VFXDatabase _vfxDatabase;
    private readonly Transform _poolParent;
    private readonly List<VFXController> _active = new();
    private readonly Dictionary<string, MonoObjectPool<VFXController>> _pools = new();

    public VFXService(SO_VFXDatabase vfxDatabase_, Transform gameRootTransform_)
    {
        _vfxDatabase = vfxDatabase_;

        var root = new GameObject("VFXRoot");
        root.transform.SetParent(gameRootTransform_);
        _poolParent = root.transform;
    }

    #region 播放指定特效
    /// <summary>在 position_ 处播放特效，可选跟随 followTarget_。</summary>
    public void PlayVFX(string vfxName_, Vector3 position_, Transform followTarget_ = null)
    {
        VFXController prefab = _vfxDatabase.GetController(vfxName_);
        if (prefab == null) return;

        VFXController controller = GetPool(vfxName_, prefab).Get();
        controller.Initialize(vfxName_, position_, followTarget_);
        controller.BindService(this);
        _active.Add(controller);
    }

    /// <summary>取或建指定名的对象池（上限读 SO 配置）。</summary>
    private MonoObjectPool<VFXController> GetPool(string vfxName_, VFXController prefab_)
    {
        if (!_pools.TryGetValue(vfxName_, out var pool))
        {
            pool = new MonoObjectPool<VFXController>(prefab_, _poolParent,
                onRelease_: controller_ => controller_.ClearForReuse(),
                maxSize_: _vfxDatabase.MaxPoolPerVfx);
            _pools[vfxName_] = pool;
        }
        return pool;
    }
    #endregion

    #region 停止播放
    /// <summary>停止并回收所有指定名字的 VFX。</summary>
    public void StopVFX(string vfxName_)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            VFXController controller = _active[i];
            if (controller.VfxName == vfxName_)
            {
                _active.RemoveAt(i);
                controller.Recycle();
            }
        }
    }
    #endregion

    #region 活跃实例回收
    /// <summary>自然播完/手动停止的实例从活跃列表移除并归还对象池（超上限由池销毁）。</summary>
    public void Release(VFXController controller_)
    {
        _active.Remove(controller_);
        if (_pools.TryGetValue(controller_.VfxName, out var pool))
            pool.Release(controller_);
    }
    #endregion
}

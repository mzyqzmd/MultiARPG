using System;
using UnityEngine.Pool;

/// <summary>纯 C# 通用对象池（仅服务非 MonoBehaviour 对象，与 MonoObjectPool 互不干扰）。内部用 UnityEngine.Pool.ObjectPool，借取/归还复杂度 O(1)，归还仅清空引用无 GC。</summary>
public class PlainObjectPool<T> where T : class
{
    private readonly IObjectPool<T> _pool;
    private readonly Action<T> _onRelease;

    /// <summary>构造一个纯 C# 对象池。createFunc_ 负责新建实例；onGet_ 借取时回调；onRelease_ 归还时回调；defaultCapacity_/maxSize_ 控制池容量。</summary>
    public PlainObjectPool(Func<T> createFunc_, Action<T> onGet_ = null, Action<T> onRelease_ = null,
        int defaultCapacity_ = 5, int maxSize_ = 20)
    {
        _onRelease = onRelease_;
        _pool = new ObjectPool<T>(createFunc_, onGet_, OnRelease, null, true, defaultCapacity_, maxSize_);
    }

    #region 对外 API
    public T Get() => _pool.Get();
    public void Release(T object_) => _pool.Release(object_);
    public void Clear() => _pool.Clear();
    #endregion

    #region 归还回调
    private void OnRelease(T object_) => _onRelease?.Invoke(object_);
    #endregion
}

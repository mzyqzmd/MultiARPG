using System;
using UnityEngine;
using UnityEngine.Pool;
public class MonoObjectPool<T> where T : MonoBehaviour
{
    public IObjectPool<T> Pool { get; private set; }
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onRelease;
    private readonly Action<T> _onDestroy;

    public MonoObjectPool(T prefab_, Transform parent_, Action<T> onGet_ = null,
        Action<T> onRelease_ = null, Action<T> onDestroy_ = null, int defaultCapacity_ = 5, int maxSize_ = 10)
    {
        _prefab = prefab_;
        _parent = parent_;
        _onGet = onGet_;
        _onRelease = onRelease_;
        _onDestroy = onDestroy_;

        Pool = new ObjectPool<T>(CreateObject, OnGet, OnRelease,
            OnDestroy, true, defaultCapacity_, maxSize_);
    }

    public T Get() => Pool.Get();
    public void Release(T object_) => Pool.Release(object_);
    public void Clear() => Pool.Clear();

    #region 创建新对象
    private T CreateObject()
    {
        T newObject = UnityEngine.Object.Instantiate(_prefab, _parent);
        newObject.gameObject.SetActive(false);
        return newObject;
    }
    #endregion

    #region 获取对象时的回调
    private void OnGet(T object_)
    {
        object_.gameObject.SetActive(true);
        _onGet?.Invoke(object_);
    }
    #endregion

    #region 放回对象时的回调
    private void OnRelease(T object_)
    {
        object_.gameObject.SetActive(false);
        _onRelease?.Invoke(object_);
    }
    #endregion

    #region 删除对象时的回调
    private void OnDestroy(T object_)
    {
        UnityEngine.Object.Destroy(object_.gameObject);
        _onDestroy?.Invoke(object_);
    }
    #endregion
}
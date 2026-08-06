using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局每帧驱动源（Utility 工具类）。懒汉式 MonoBehaviour 单例，自管生命周期，
/// 不进 Services 容器、不写入 GameRoot。普通类（如 EventTimer）构造时向本服务
/// 登记自己的 Tick 方法，即可被动获得每帧推进，无需调用方手动驱动。
/// </summary>
public class TickService : MonoBehaviour
{
    public static TickService Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("TickService");
                _instance = go.AddComponent<TickService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private static TickService _instance;

    private readonly List<Action<float>> _tickCallbacks = new();

    #region 生命周期
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
    #endregion

    #region 回调登记
    public void Register(Action<float> cb_) => _tickCallbacks.Add(cb_);

    public void Unregister(Action<float> cb_) => _tickCallbacks.Remove(cb_);
    #endregion

    #region 每帧驱动
    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _tickCallbacks.Count; i++)
            _tickCallbacks[i](dt);
    }
    #endregion
}

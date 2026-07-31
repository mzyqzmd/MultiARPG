using System;
using System.Collections.Generic;

/// <summary>
/// 轻量服务容器，作为组合根（GameRoot）与各消费方之间的依赖注入桥梁。
/// 仅用于登记"客户端真正全局"的服务（输入、事件总线、游戏状态等），
/// 不用于每实例对象（玩家/敌人）。
/// </summary>
public static class Services
{
    private static readonly Dictionary<Type, IService> _registry = new();

    public static void Register<T>(T service_) where T : class, IService =>
        _registry[typeof(T)] = service_;

    public static void Unregister<T>() where T : class, IService =>
        _registry.Remove(typeof(T));

    public static T Get<T>() where T : class, IService =>
        _registry.TryGetValue(typeof(T), out var service_) ? (T)service_ : null;

    public static bool TryGet<T>(out T service_) where T : class, IService
    {
        if (_registry.TryGetValue(typeof(T), out var obj_))
        {
            service_ = (T)obj_;
            return true;
        }
        service_ = null;
        return false;
    }

    public static void Clear() => _registry.Clear();
}

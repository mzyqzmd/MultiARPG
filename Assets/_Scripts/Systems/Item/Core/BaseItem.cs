using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>场景中的物品实例：持有一个对应的 SO_BaseItem 数据引用，外界经 ItemData 取数据调 Use。</summary>
public abstract class BaseItem<T> : MonoBehaviour where T : SO_BaseItem
{
    [SerializeField] protected T _itemData;
    [SerializeField, Tooltip("武器持有者（联机时注册攻击者）。不注册则作为无来源环境伤害")]
    protected Player _owner;

    public T ItemData => _itemData;
    public Player Owner => _owner;

    public void RegisterOwner(Player owner_) => _owner = owner_;

    public virtual void UseItem(IEffectTarget target_, IEffectSource source_) =>
        _itemData.UseItem(target_, source_);
}

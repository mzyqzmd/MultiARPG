using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public abstract class SO_BaseItem : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("物品唯一标识：自动同步为资产文件名。")]
    [SerializeField] protected string _itemID;
    [SerializeField] protected Sprite _itemSprite;
    [TextArea(3, 10)] protected string _itemDescription;

    /// <summary>武器/药壶等子类 override 实现具体行为。</summary>
    public abstract void UseItem(IEffectTarget target_, IEffectSource source_);

    private void OnValidate() => _itemID = name;
}

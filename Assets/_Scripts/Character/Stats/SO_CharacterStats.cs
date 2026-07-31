using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SO_CharacterStats : ScriptableObject
{
    [Header("角色通用配置")]
    [Tooltip("步行基础移动速度（米/秒）。不按跑步键时的常规移动速度。")]
    [SerializeField] protected float _normalMoveSpeed = 2f;
    [Tooltip("角色转向速度（度/秒）。数值越大转身越快。")]
    [SerializeField] protected float _rotationSpeed = 500f;
    [Tooltip("角色生命上限。生命归零即死亡，经效果系统（伤害 / 回复）读写。")]
    [SerializeField] protected int _maxHealth = 100;

    public float NormalMoveSpeed => _normalMoveSpeed;
    public float RotationSpeed => _rotationSpeed;
    public int MaxHealth => _maxHealth;
}

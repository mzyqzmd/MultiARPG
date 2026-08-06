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
    [Tooltip("受击硬直时长（秒）。角色被击中后停留在受击态的时长，可被硬直延长类效果经 EStatType.HitDuration 运行时修正。")]
    [SerializeField] protected float _hitDuration = 0.4f;

    public float NormalMoveSpeed => _normalMoveSpeed;
    public float RotationSpeed => _rotationSpeed;
    public int MaxHealth => _maxHealth;
    public float HitDuration => _hitDuration;
}

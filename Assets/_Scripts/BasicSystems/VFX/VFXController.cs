using UnityEngine;

/// <summary>
/// VFX 预制体上的发射器脚本。仅负责播放、可选跟随目标、自然播完或手动停止后自销毁。
/// </summary>
public class VFXController : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particle;

    private VFXService _service;
    public string VfxName { get; private set; }

    private Transform _followTarget;

    /// <summary>绑定所属服务，用于自然销毁时从活跃列表移除。</summary>
    public void BindService(VFXService service_) => _service = service_;

    /// <summary>在 position_ 生成并播放，可选跟随 followTarget_。</summary>
    public void Initialize(string vfxName_, Vector3 position_, Transform followTarget_ = null)
    {
        VfxName = vfxName_;
        _followTarget = followTarget_;
        transform.position = position_;
        _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _particle.Play();
    }

    #region 生命周期驱动
    private void Update()
    {
        if (_followTarget != null)
        {
            transform.position = _followTarget.position;
            return;
        }
        if (_particle.isStopped)
            Recycle();
    }
    #endregion

    #region 播放控制
    /// <summary>停止并回收入池（供 VFXService.StopVFX 调用）。</summary>
    public void Recycle() => _service.Release(this);

    /// <summary>入池前清空状态，确保复用实例无残留粒子与跟随目标。</summary>
    public void ClearForReuse()
    {
        _followTarget = null;
        _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    #endregion
}

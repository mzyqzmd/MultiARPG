#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Gizmo 编辑器可视化静态工具类。集中封装线框球、视锥扇形、FOV 边界等通用绘制，
/// 供各 MonoBehaviour 在 OnDrawGizmos 内以静态方法调用，避免重复绘制代码。
/// 整体以 #if UNITY_EDITOR 包裹，发布构建零开销；调用方仍须在自己的 OnDrawGizmos 内调用。
/// 角度参数统一为半角（halfAngle），调用方负责将原全角/半角字段转换为半角传入。
/// </summary>
public static class GizmoDrawer
{
    #region 线框球
    /// <summary>绘制线框球（纯描边，无填充）。</summary>
    public static void DrawWireSphere(Vector3 center_, float radius_, Color color_)
    {
        Gizmos.color = color_;
        Gizmos.DrawWireSphere(center_, radius_);
    }
    #endregion

    #region 视锥扇形（完整：扇形面 + 中心轴 + 两边界 + 端点弧）
    /// <summary>绘制完整视锥：扇形面 + 中心轴 + 左右边界 + 视距端点弧。
    /// halfAngle_ 为半角（全角须先 /2）；segments_ 为扇形细分段数（默认 32）。</summary>
    public static void DrawVisionCone(Vector3 origin_, Vector3 forward_, float distance_, float halfAngle_, Color color_, int segments_ = 32)
    {
        float halfAngle = halfAngle_;
        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);
        Vector3 leftDir = leftRot * forward_;
        Vector3 rightDir = rightRot * forward_;

        // 扇形面（半透明）
        Gizmos.color = new Color(color_.r, color_.g, color_.b, 0.12f);
        Vector3 prev = origin_;
        for (int i = 1; i <= segments_; i++)
        {
            float t = (float)i / segments_;
            float ang = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 d = Quaternion.AngleAxis(ang, Vector3.up) * forward_;
            Vector3 cur = origin_ + d * distance_;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }

        // 中心视轴（实线）
        Gizmos.color = color_;
        Gizmos.DrawLine(origin_, origin_ + forward_ * distance_);
        // 两边界（半透明）
        Gizmos.color = new Color(color_.r, color_.g, color_.b, 0.5f);
        Gizmos.DrawLine(origin_, origin_ + leftDir * distance_);
        Gizmos.DrawLine(origin_, origin_ + rightDir * distance_);

        // 视距端点弧
        Gizmos.color = new Color(color_.r, color_.g, color_.b, 0.3f);
        Vector3 last = origin_ + leftDir * distance_;
        for (int i = 1; i <= segments_; i++)
        {
            float t = (float)i / segments_;
            float ang = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 d = Quaternion.AngleAxis(ang, Vector3.up) * forward_;
            Vector3 cur = origin_ + d * distance_;
            Gizmos.DrawLine(last, cur);
            last = cur;
        }
    }
    #endregion

    #region FOV 边界（仅左右两条边界线）
    /// <summary>绘制以 origin_ 为顶点、forward_ 为轴的扇形两条边界线（无扇形面/端弧），对应相机前方 FOV 轮廓。
    /// halfAngle_ 为半角（全角须先 /2）。</summary>
    public static void DrawFovBoundary(Vector3 center_, Vector3 forward_, float halfAngle_, float distance_, Color color_)
    {
        Gizmos.color = color_;
        var left = Quaternion.AngleAxis(-halfAngle_, Vector3.up) * forward_;
        var right = Quaternion.AngleAxis(halfAngle_, Vector3.up) * forward_;
        Gizmos.DrawLine(center_, center_ + left * distance_);
        Gizmos.DrawLine(center_, center_ + right * distance_);
    }
    #endregion
}
#endif

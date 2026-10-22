using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct PowerPortRenderData
{
    // Three float4 values: C# and HLSL stride is exactly 48 bytes.
    public Vector4 PositionSize;
    public Vector4 Tint;
    public Vector4 Motion; // from Y, target Y, start (unscaled), duration
    public Vector3 Center => new(PositionSize.x, PositionSize.y, PositionSize.z);
    public float Offset(float now)
    {
        float t = Motion.w <= 0f ? 1f : Mathf.Clamp01((now - Motion.z) / Motion.w);
        return Mathf.Lerp(Motion.x, Motion.y, t * t * (3f - 2f * t));
    }
}

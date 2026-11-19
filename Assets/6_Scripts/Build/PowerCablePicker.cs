using System.Collections.Generic;
using UnityEngine;

public static class PowerCablePicker
{
    private static readonly List<PowerCable> active = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => active.Clear();

    internal static void Register(PowerCable cable) => active.Add(cable);
    internal static void Unregister(PowerCable cable) => active.Remove(cable);

    public static PowerCable Pick(Camera camera, Vector2 pointer, float maxDepth, out Vector3 point)
    {
        point = default;
        if (!camera || !camera.pixelRect.Contains(pointer)) return null;
        float bestDistance = 64f;
        float bestDepth = float.PositiveInfinity;
        PowerCable closest = null;
        foreach (var cable in active)
        {
            if (!cable || !cable.SourceNode || !cable.TargetNode ||
                !cable.SourcePort?.Anchor || !cable.TargetPort?.Anchor) continue;
            Vector3 start = cable.SourcePort.Anchor.position, end = cable.TargetPort.Anchor.position;
            Vector3 a = camera.WorldToScreenPoint(start), b = camera.WorldToScreenPoint(end);
            if (a.z < camera.nearClipPlane || b.z < camera.nearClipPlane) continue;
            Vector2 delta = (Vector2)(b - a);
            float t = Mathf.Clamp01(Vector2.Dot(pointer - (Vector2)a, delta) / Mathf.Max(delta.sqrMagnitude, .0001f));
            float distance = ((Vector2)a + t * delta - pointer).sqrMagnitude;
            float depth = camera.orthographic ? Mathf.Lerp(a.z, b.z, t) : 1f / Mathf.Lerp(1f / a.z, 1f / b.z, t);
            if (depth > maxDepth || distance > bestDistance || (Mathf.Approximately(distance, bestDistance) && depth >= bestDepth)) continue;
            bestDistance = distance;
            bestDepth = depth;
            closest = cable;
            point = Vector3.Lerp(start, end, camera.orthographic ? t : t * depth / b.z);
        }
        return closest;
    }
}

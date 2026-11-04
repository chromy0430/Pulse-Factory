using UnityEngine;

public static class DemolitionPicker
{
    public static ConstructionReceipt Pick(Camera camera, Vector2 pointer)
    {
        if (!camera || !camera.pixelRect.Contains(pointer)) return null;
        var ray = camera.ScreenPointToRay(pointer);
        ConstructionReceipt building = null;
        float visibleDepth = float.PositiveInfinity;
        if (Physics.Raycast(ray, out var hit, camera.farClipPlane, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            building = hit.collider.GetComponentInParent<ConstructionReceipt>();
            visibleDepth = camera.WorldToScreenPoint(hit.point).z;
        }
        float best = 8f * 8f;
        ConstructionReceipt closest = null;
        foreach (var receipt in ConstructionReceipt.Cables)
        {
            var cable = receipt.Cable;
            if (!receipt.Available || !cable || !cable.isActiveAndEnabled || !cable.SourcePort.Anchor || !cable.TargetPort.Anchor) continue;
            Vector3 a = camera.WorldToScreenPoint(cable.SourcePort.Anchor.position);
            Vector3 b = camera.WorldToScreenPoint(cable.TargetPort.Anchor.position);
            if (a.z < camera.nearClipPlane || b.z < camera.nearClipPlane) continue;
            Vector2 segment = (Vector2)(b - a);
            float t = Mathf.Clamp01(Vector2.Dot(pointer - (Vector2)a, segment) / Mathf.Max(segment.sqrMagnitude, .0001f));
            float depth = camera.orthographic ? Mathf.Lerp(a.z, b.z, t) : 1f / Mathf.Lerp(1f / a.z, 1f / b.z, t);
            if (depth > visibleDepth + .05f) continue;
            float distance = ((Vector2)a + t * segment - pointer).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            closest = receipt;
        }
        return closest ? closest : building;
    }
}

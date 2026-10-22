using System.Collections.Generic;
using UnityEngine;

// Presentation coordinates are independent of immutable cable anchors.
public sealed class PowerPortPlacement
{
    private readonly Dictionary<PowerNode, Bounds> models = new();

    public void Clear() => models.Clear();
    public void Register(PowerNode owner)
    {
        Bounds localBounds = default;
        bool found = false;
        foreach (var renderer in owner.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !renderer.enabled) continue;
            var box = renderer.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = box.center + Vector3.Scale(box.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                corner = owner.transform.InverseTransformPoint(corner);
                if (!found) { localBounds = new Bounds(corner, Vector3.zero); found = true; }
                else localBounds.Encapsulate(corner);
            }
        }
        if (found) models[owner] = localBounds;
    }

    public float Height(PowerNode owner, bool aboveModel, float clearance, float halfSize, float drop)
    {
        float top = float.NegativeInfinity;
        // Follow the building transform, not transient POP animation of its model child.
        if (aboveModel && models.TryGetValue(owner, out var box))
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = box.center + Vector3.Scale(box.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                top = Mathf.Max(top, owner.transform.TransformPoint(corner).y);
            }
        return top + clearance + halfSize + Mathf.Abs(drop);
    }

    public Vector3 Center(PowerNode owner, Transform anchor, float minimumY, float height, float padding)
    {
        Vector3 local = owner.transform.InverseTransformPoint(anchor.position);
        local.y = 0;
        float extent = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z));
        Vector3 outward = extent > 0.0001f ? owner.transform.TransformDirection(local / extent) : Vector3.zero;
        outward.y = 0;
        Vector3 center = anchor.position + outward * padding;
        center.y = Mathf.Max(anchor.position.y + minimumY, height);
        return center;
    }
}

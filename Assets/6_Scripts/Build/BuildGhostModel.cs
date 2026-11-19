using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Build a display-only hierarchy. Gameplay scripts, colliders and animators never enter the board.
public sealed class BuildGhostModel : MonoBehaviour
{
    private readonly List<MeshRenderer> renderers = new();
    private readonly List<Mesh> baked = new();
    private Material currentMaterial;

    public void Build(GameObject prefab)
    {
        var root = prefab.transform;
        var visual = root.Find("Visual");
        var source = visual ? visual : root;
        var transforms = new Dictionary<Transform, Transform> { [root] = transform };
        transform.localScale = root.localScale;
        var lowerLods = new HashSet<Renderer>();
        foreach (var group in source.GetComponentsInChildren<LODGroup>())
        {
            var lods = group.GetLODs();
            for (int i = 1; i < lods.Length; i++)
                foreach (var item in lods[i].renderers) lowerLods.Add(item);
            if (lods.Length > 0)
                foreach (var item in lods[0].renderers) lowerLods.Remove(item);
        }
        foreach (var original in source.GetComponentsInChildren<Renderer>())
        {
            if (!original.enabled || lowerLods.Contains(original)) continue;
            Mesh mesh;
            if (original is MeshRenderer && original.TryGetComponent<MeshFilter>(out var filter))
                mesh = filter.sharedMesh;
            else if (original is SkinnedMeshRenderer skin && skin.sharedMesh)
            {
                mesh = new Mesh();
                skin.BakeMesh(mesh);
                baked.Add(mesh);
            }
            else continue;
            if (!mesh || mesh.subMeshCount == 0) continue;
            var part = Mirror(original.transform, transforms).gameObject;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderers.Add(renderer);
        }
    }

    private static Transform Mirror(Transform original, Dictionary<Transform, Transform> transforms)
    {
        if (transforms.TryGetValue(original, out var copy)) return copy;
        var parent = Mirror(original.parent, transforms);
        copy = new GameObject(original.name).transform;
        copy.SetParent(parent, false);
        copy.SetLocalPositionAndRotation(original.localPosition, original.localRotation);
        copy.localScale = original.localScale;
        transforms.Add(original, copy);
        return copy;
    }

    private readonly Dictionary<(Material, int), Material[]> slots = new();
    public void SetTint(Material material)
    {
        if (currentMaterial == material) return;
        currentMaterial = material;
        foreach (var renderer in renderers)
        {
            int count = renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount;
            if (!slots.TryGetValue((material, count), out var materials))
            {
                materials = new Material[count];
                System.Array.Fill(materials, material);
                slots.Add((material, count), materials);
            }
            renderer.sharedMaterials = materials;
        }
    }

    private void OnDestroy()
    {
        foreach (var mesh in baked) Destroy(mesh);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Place on the root of an existing static MeshRenderer model. At runtime, it
/// replaces the visible triangle surfaces with feature-edge hologram lines.
/// Its original Mesh objects are never modified.
///
/// Supports MeshFilter + MeshRenderer (not SkinnedMeshRenderer).
/// Source model imports must have Read/Write Enabled checked.
/// </summary>
[DisallowMultipleComponent]
public sealed class HologramWireframe : MonoBehaviour
{
    [Header("Material (optional: auto-created from included shader)")]
    [SerializeField] private Material hologramMaterial;

    [Header("Feature Edge Extraction")]
    [SerializeField, Range(0f, 180f)] private float creaseAngle = 32f;
    [SerializeField, Min(0.0001f)] private float weldDistance = 0.01f;
    [SerializeField] private bool hideOriginalMeshes = true;

    [Header("Visuals")]
    [SerializeField, ColorUsage(true, true)] private Color glowColor =
        new Color(0.20f, 0.95f, 1f, 1f);
    [SerializeField, Range(0f, 20f)] private float glowIntensity = 5f;

    // Generated instances follow their MeshFilter parents, including animation
    // that moves or rotates whole GameObjects.
    private readonly List<GameObject> generatedObjects = new List<GameObject>();
    private readonly Dictionary<Mesh, Mesh> generatedMeshCache = new Dictionary<Mesh, Mesh>();
    private readonly Dictionary<MeshRenderer, bool> originalStates =
        new Dictionary<MeshRenderer, bool>();
    private readonly List<MeshRenderer> hologramRenderers = new List<MeshRenderer>();

    private Material runtimeMaterial;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    /// <summary>Call after replacing a source mesh or changing crease/weld settings.</summary>
    [ContextMenu("Rebuild Hologram")]
    public void Rebuild()
    {
        Cleanup();

        if (!isActiveAndEnabled)
            return;

        Material material = hologramMaterial;
        if (material == null)
        {
            Shader shader = Shader.Find("HoloTactical/URP/WireframeGlow");
            if (shader == null)
            {
                Debug.LogError("[HologramWireframe] Missing HoloWireframeGlow.shader. " +
                               "Import the shader into Assets first.", this);
                return;
            }

            runtimeMaterial = new Material(shader)
            {
                name = "Holo Wireframe (Runtime)"
            };
            material = runtimeMaterial;
        }

        // Snapshot before generating child objects. Otherwise the new MeshFilters
        // might be treated as source objects on a subsequent rebuild.
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        int created = 0;

        foreach (MeshFilter sourceFilter in filters)
        {
            if (sourceFilter == null || !sourceFilter.gameObject.activeInHierarchy)
                continue;

            MeshRenderer originalRenderer = sourceFilter.GetComponent<MeshRenderer>();
            Mesh sourceMesh = sourceFilter.sharedMesh;
            if (originalRenderer == null || !originalRenderer.enabled || sourceMesh == null)
                continue;

            if (!sourceMesh.isReadable)
            {
                Debug.LogWarning($"[HologramWireframe] '{sourceMesh.name}' is not readable. " +
                                 "Select the source FBX/model, enable Read/Write in the " +
                                 "Model import settings, and click Apply.", sourceFilter);
                continue; // Keep the original mesh visible instead of losing it.
            }

            if (!generatedMeshCache.TryGetValue(sourceMesh, out Mesh lineMesh))
            {
                lineMesh = CreateFeatureLineMesh(sourceMesh, creaseAngle, weldDistance);
                generatedMeshCache.Add(sourceMesh, lineMesh);
            }

            if (lineMesh == null || lineMesh.vertexCount == 0)
                continue;

            GameObject lines = new GameObject("[Hologram] " + sourceFilter.name);
            lines.layer = sourceFilter.gameObject.layer;
            lines.transform.SetParent(sourceFilter.transform, false);
            lines.transform.localPosition = Vector3.zero;
            lines.transform.localRotation = Quaternion.identity;
            lines.transform.localScale = Vector3.one;

            MeshFilter outputFilter = lines.AddComponent<MeshFilter>();
            outputFilter.sharedMesh = lineMesh;

            MeshRenderer outputRenderer = lines.AddComponent<MeshRenderer>();
            outputRenderer.sharedMaterial = material;
            outputRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outputRenderer.receiveShadows = false;
            outputRenderer.lightProbeUsage = LightProbeUsage.Off;
            outputRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            generatedObjects.Add(lines);
            hologramRenderers.Add(outputRenderer);

            if (hideOriginalMeshes)
            {
                originalStates.Add(originalRenderer, originalRenderer.enabled);
                originalRenderer.enabled = false;
            }

            created++;
        }

        ApplyStyle();
        if (created == 0)
            Debug.LogWarning("[HologramWireframe] No lines created. Check that your " +
                             "model has active MeshFilter + MeshRenderer components and " +
                             "readable triangle meshes.", this);
    }

    /// <summary>Change tint and intensity without rebuilding geometry.</summary>
    public void SetStyle(Color color, float intensity)
    {
        glowColor = color;
        glowIntensity = Mathf.Clamp(intensity, 0f, 20f);
        ApplyStyle();
    }

    private void ApplyStyle()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        propertyBlock.SetColor(GlowColorId, glowColor);
        propertyBlock.SetFloat(GlowIntensityId, glowIntensity);

        foreach (MeshRenderer renderer in hologramRenderers)
        {
            if (renderer != null)
                renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void OnValidate()
    {
        // Inspector colour/intensity edits take effect during Play Mode.
        if (Application.isPlaying)
            ApplyStyle();
    }

    private void Cleanup()
    {
        foreach (KeyValuePair<MeshRenderer, bool> state in originalStates)
        {
            if (state.Key != null)
                state.Key.enabled = state.Value;
        }
        originalStates.Clear();

        foreach (GameObject child in generatedObjects)
        {
            if (child == null)
                continue;

            child.SetActive(false); // Hide immediately; Destroy is deferred.
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
        generatedObjects.Clear();
        hologramRenderers.Clear();

        foreach (Mesh mesh in generatedMeshCache.Values)
        {
            if (mesh == null)
                continue;

            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
        generatedMeshCache.Clear();

        if (runtimeMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeMaterial);
            else
                DestroyImmediate(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    private struct EdgeState
    {
        public Vector3 firstFaceNormal;
        public int faceCount;
        public bool sharp;
    }

    private static Mesh CreateFeatureLineMesh(Mesh source, float creaseDegrees, float weld)
    {
        Vector3[] vertices = source.vertices;
        Dictionary<Vector3Int, int> welded = new Dictionary<Vector3Int, int>(vertices.Length);
        List<Vector3> weldedPositions = new List<Vector3>(vertices.Length);
        int[] weldedIds = new int[vertices.Length];

        float invWeld = 1f / Mathf.Max(0.0001f, weld);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = vertices[i];
            Vector3Int key = new Vector3Int(
                Mathf.RoundToInt(p.x * invWeld),
                Mathf.RoundToInt(p.y * invWeld),
                Mathf.RoundToInt(p.z * invWeld));

            if (!welded.TryGetValue(key, out int id))
            {
                id = weldedPositions.Count;
                welded.Add(key, id);
                weldedPositions.Add(p);
            }
            weldedIds[i] = id;
        }

        float dotThreshold = Mathf.Cos(creaseDegrees * Mathf.Deg2Rad);
        Dictionary<ulong, EdgeState> edges = new Dictionary<ulong, EdgeState>();

        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            if (source.GetTopology(sub) != MeshTopology.Triangles)
                continue;

            // GetTriangles applies submesh baseVertex by default.
            int[] triangles = source.GetTriangles(sub);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                int a = weldedIds[triangles[i]];
                int b = weldedIds[triangles[i + 1]];
                int c = weldedIds[triangles[i + 2]];
                if (a == b || b == c || c == a)
                    continue;

                Vector3 normal = Vector3.Cross(
                    weldedPositions[b] - weldedPositions[a],
                    weldedPositions[c] - weldedPositions[a]);

                if (normal.sqrMagnitude < 1e-12f)
                    continue;

                normal.Normalize();
                AccumulateEdge(edges, a, b, normal, dotThreshold);
                AccumulateEdge(edges, b, c, normal, dotThreshold);
                AccumulateEdge(edges, c, a, normal, dotThreshold);
            }
        }

        List<Vector3> lineVertices = new List<Vector3>();
        foreach (KeyValuePair<ulong, EdgeState> entry in edges)
        {
            if (entry.Value.faceCount != 1 && !entry.Value.sharp)
                continue;

            int a = (int)(entry.Key >> 32);
            int b = (int)(entry.Key & 0xFFFFFFFFUL);
            lineVertices.Add(weldedPositions[a]);
            lineVertices.Add(weldedPositions[b]);
        }

        Mesh output = new Mesh { name = source.name + " (Hologram Lines)" };
        output.indexFormat = lineVertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        output.SetVertices(lineVertices);

        int[] indices = new int[lineVertices.Count];
        for (int i = 0; i < indices.Length; i++)
            indices[i] = i;

        output.SetIndices(indices, MeshTopology.Lines, 0, true);
        return output;
    }

    private static void AccumulateEdge(
        Dictionary<ulong, EdgeState> edges,
        int a,
        int b,
        Vector3 normal,
        float dotThreshold)
    {
        uint low = (uint)Mathf.Min(a, b);
        uint high = (uint)Mathf.Max(a, b);
        ulong key = ((ulong)low << 32) | high;

        if (!edges.TryGetValue(key, out EdgeState state))
        {
            edges.Add(key, new EdgeState
            {
                firstFaceNormal = normal,
                faceCount = 1,
                sharp = false
            });
            return;
        }

        state.faceCount++;
        if (Vector3.Dot(state.firstFaceNormal, normal) < dotThreshold)
            state.sharp = true;
        edges[key] = state;
    }
}

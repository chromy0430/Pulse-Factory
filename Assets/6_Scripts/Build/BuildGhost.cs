using UnityEngine;

public sealed class BuildGhost : MonoBehaviour
{
    private BuildGhostModel body;
    private MeshRenderer gridRenderer;
    private MaterialPropertyBlock gridTint;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private Material gridMaterial;
    private Mesh gridMesh;
    private Material validMaterial;
    private Material invalidMaterial;
    private Vector3[] vertices;
    private int stepsX, stepsZ;
    private Vector3 lastPosition = new Vector3(float.PositiveInfinity, 0f, 0f);

    public void Initialize(BuildingDefinitionSO definition, Material valid, Material invalid, float cellSize = 0.1f)
    {
        Vector2 footprint = definition.PlacementSize;
        gridTint = new MaterialPropertyBlock();
        validMaterial = valid;
        invalidMaterial = invalid;
        stepsX = Mathf.Max(10, Mathf.CeilToInt(footprint.x / (2f * cellSize)) + 1);
        stepsZ = Mathf.Max(10, Mathf.CeilToInt(footprint.y / (2f * cellSize)) + 1);
        vertices = new Vector3[4 * (stepsX + stepsZ + 1)];
        var preview = new GameObject("Preview");
        preview.transform.SetParent(transform, false);
        body = preview.AddComponent<BuildGhostModel>();
        body.Build(definition.Prefab);
        body.SetTint(valid);
        var grid = new GameObject("LocalGrid", typeof(MeshFilter), typeof(MeshRenderer));
        grid.transform.SetParent(transform, false);
        gridMesh = new Mesh { name = "Build preview grid" };
        gridMesh.MarkDynamic();
        int[] indices = new int[vertices.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        gridMesh.vertices = vertices;
        gridMesh.SetIndices(indices, MeshTopology.Lines, 0);
        grid.GetComponent<MeshFilter>().sharedMesh = gridMesh;
        gridRenderer = grid.GetComponent<MeshRenderer>();
        gridRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public void Refresh(BuildGrid grid, Vector3 position, bool valid)
    {
        transform.SetPositionAndRotation(position, grid.Rotation);
        var material = valid ? validMaterial : invalidMaterial;
        body.SetTint(material);
        if (gridMaterial != material)
        {
            gridMaterial = material;
            gridRenderer.sharedMaterial = material;
            if (material && material.HasProperty(BaseColor))
            {
                var color = material.GetColor(BaseColor) * .42f;
                gridTint.SetColor(BaseColor, color);
                gridRenderer.SetPropertyBlock(gridTint);
            }
        }
        if (position == lastPosition) return;
        lastPosition = position;
        Vector3 offset = Quaternion.Inverse(grid.Rotation) * (position - grid.SurfaceCenter);
        Vector2 half = grid.HalfSize;
        float minX = Mathf.Max(-stepsX * grid.CellSize, -half.x - offset.x);
        float maxX = Mathf.Min(stepsX * grid.CellSize, half.x - offset.x);
        float minZ = Mathf.Max(-stepsZ * grid.CellSize, -half.y - offset.z);
        float maxZ = Mathf.Min(stepsZ * grid.CellSize, half.y - offset.z);
        gridRenderer.enabled = minX <= maxX && minZ <= maxZ;
        if (!gridRenderer.enabled) return;
        int index = 0;
        for (int i = -stepsX; i <= stepsX; i++)
        {
            float x = Mathf.Clamp(i * grid.CellSize, minX, maxX);
            vertices[index++] = new Vector3(x, 0.015f, minZ);
            vertices[index++] = new Vector3(x, 0.015f, maxZ);
        }
        for (int i = -stepsZ; i <= stepsZ; i++)
        {
            float z = Mathf.Clamp(i * grid.CellSize, minZ, maxZ);
            vertices[index++] = new Vector3(minX, 0.015f, z);
            vertices[index++] = new Vector3(maxX, 0.015f, z);
        }
        gridMesh.vertices = vertices;
        gridMesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (gridMesh != null) Destroy(gridMesh);
    }
}

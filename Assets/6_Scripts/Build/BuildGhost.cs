using UnityEngine;

public sealed class BuildGhost : MonoBehaviour
{
    private MeshRenderer body;
    private MeshRenderer gridRenderer;
    private Mesh gridMesh;
    private Material validMaterial;
    private Material invalidMaterial;
    private readonly Vector3[] vertices = new Vector3[84];
    private Vector3 lastPosition = new Vector3(float.PositiveInfinity, 0f, 0f);

    public void Initialize(Vector2 footprint, Material valid, Material invalid)
    {
        validMaterial = valid;
        invalidMaterial = invalid;
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Preview";
        cube.transform.SetParent(transform, false);
        cube.transform.localPosition = Vector3.up * 0.5f;
        cube.transform.localScale = new Vector3(footprint.x, 1f, footprint.y);
        var collider = cube.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        body = cube.GetComponent<MeshRenderer>();
        body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        body.receiveShadows = false;
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
        gridRenderer.sharedMaterial = valid;
        gridRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public void Refresh(BuildGrid grid, Vector3 position, bool valid)
    {
        transform.SetPositionAndRotation(position, grid.Rotation);
        body.sharedMaterial = valid ? validMaterial : invalidMaterial;
        gridRenderer.sharedMaterial = body.sharedMaterial;
        if (position == lastPosition) return;
        lastPosition = position;
        Vector3 offset = Quaternion.Inverse(grid.Rotation) * (position - grid.SurfaceCenter);
        Vector2 half = grid.HalfSize;
        float radius = grid.CellSize * 10f;
        float minX = Mathf.Max(-radius, -half.x - offset.x);
        float maxX = Mathf.Min(radius, half.x - offset.x);
        float minZ = Mathf.Max(-radius, -half.y - offset.z);
        float maxZ = Mathf.Min(radius, half.y - offset.z);
        gridRenderer.enabled = minX <= maxX && minZ <= maxZ;
        if (!gridRenderer.enabled) return;
        for (int i = 0; i <= 20; i++)
        {
            float p = (i - 10) * grid.CellSize;
            float x = Mathf.Clamp(p, minX, maxX);
            float z = Mathf.Clamp(p, minZ, maxZ);
            vertices[i * 4] = new Vector3(x, 0.015f, minZ);
            vertices[i * 4 + 1] = new Vector3(x, 0.015f, maxZ);
            vertices[i * 4 + 2] = new Vector3(minX, 0.015f, z);
            vertices[i * 4 + 3] = new Vector3(maxX, 0.015f, z);
        }
        gridMesh.vertices = vertices;
        gridMesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (gridMesh != null) Destroy(gridMesh);
    }
}

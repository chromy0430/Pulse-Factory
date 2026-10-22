using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class PowerPortRenderer : IDisposable
{
    private static readonly int DataId = Shader.PropertyToID("_PortData");
    private static readonly int TimeId = Shader.PropertyToID("_PortTime");
    private readonly Material material;
    private readonly Mesh mesh;
    private readonly MaterialPropertyBlock properties = new();
    private GraphicsBuffer instances;
    private GraphicsBuffer commands;
    private readonly GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
    private int capacity;
    private int count;
    private bool fallbackDirty;
    private readonly List<Vector3> vertices = new();
    private readonly List<Vector2> uv = new();
    private readonly List<Vector4> motion = new();
    private readonly List<Vector2> sizes = new();
    private readonly List<Color> colors = new();
    private readonly List<int> triangles = new();
    public bool IsIndirect { get; }
    public long UploadedBytes { get; private set; }
    public int UploadCalls { get; private set; }

    public PowerPortRenderer(Material source, Shader fallback, bool forceFallback)
    {
        IsIndirect = !forceFallback && SystemInfo.supportsInstancing &&
            SystemInfo.supportsComputeShaders && SystemInfo.supportsIndirectArgumentsBuffer &&
            source.shader.isSupported;
        material = new Material(source) { name = "PowerPortIcons (Runtime)", hideFlags = HideFlags.DontSave };
        mesh = new Mesh { name = "PowerPortIcons (Shared)", hideFlags = HideFlags.DontSave };
        if (IsIndirect)
        {
            material.enableInstancing = true;
            mesh.vertices = new[] { new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0),
                new Vector3(0.5f,0.5f,0), new Vector3(-0.5f,0.5f,0) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            commands = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        }
        else
        {
            material.shader = fallback;
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.MarkDynamic();
        }
    }

    public void SetCount(int value)
    {
        count = value;
        if (!IsIndirect) { fallbackDirty = true; return; }
        if (capacity < count)
        {
            instances?.Dispose();
            capacity = Mathf.NextPowerOfTwo(Mathf.Max(8, count));
            instances = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity, 48);
            properties.SetBuffer(DataId, instances);
        }
        args[0] = new GraphicsBuffer.IndirectDrawIndexedArgs {
            indexCountPerInstance = mesh.GetIndexCount(0), instanceCount = (uint)count,
            startIndex = mesh.GetIndexStart(0), baseVertexIndex = mesh.GetBaseVertex(0), startInstance = 0 };
        commands.SetData(args);
        UploadCalls++;
        UploadedBytes += GraphicsBuffer.IndirectDrawIndexedArgs.size;
    }

    public void Upload(PowerPortRenderData[] data, int start, int length)
    {
        if (length == 0) return;
        if (IsIndirect) instances.SetData(data, start, start, length);
        else fallbackDirty = true;
        UploadCalls++;
        UploadedBytes += length * 48L;
    }

    public void Draw(Camera camera, PowerPortRenderData[] data, Bounds bounds, float now)
    {
        if (count == 0 || camera == null) return;
        properties.SetFloat(TimeId, now);
        var parameters = new RenderParams(material) {
            camera = camera, worldBounds = bounds, matProps = properties,
            shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false,
            lightProbeUsage = LightProbeUsage.Off, reflectionProbeUsage = ReflectionProbeUsage.Off };
        if (IsIndirect) Graphics.RenderMeshIndirect(parameters, mesh, commands);
        else
        {
            if (fallbackDirty) RebuildFallback(data, bounds);
            Graphics.RenderMesh(parameters, mesh, 0, Matrix4x4.identity);
        }
    }

    private void RebuildFallback(PowerPortRenderData[] data, Bounds bounds)
    {
        vertices.Clear(); uv.Clear(); motion.Clear(); sizes.Clear(); colors.Clear(); triangles.Clear();
        for (int i = 0; i < count; i++)
        {
            for (int corner = 0; corner < 4; corner++)
            {
                vertices.Add(data[i].Center);
                uv.Add(new Vector2(corner == 1 || corner == 2 ? 1 : 0, corner >= 2 ? 1 : 0));
                motion.Add(data[i].Motion);
                sizes.Add(new Vector2(data[i].PositionSize.w, 0));
                colors.Add(data[i].Tint);
            }
            int v = i * 4;
            triangles.Add(v); triangles.Add(v + 2); triangles.Add(v + 1);
            triangles.Add(v); triangles.Add(v + 3); triangles.Add(v + 2);
        }
        mesh.Clear();
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetUVs(1, motion); mesh.SetUVs(2, sizes);
        mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
        mesh.bounds = bounds;
        fallbackDirty = false;
    }

    public void Dispose()
    {
        instances?.Dispose(); commands?.Dispose();
        UnityEngine.Object.Destroy(mesh);
        UnityEngine.Object.Destroy(material);
    }
}

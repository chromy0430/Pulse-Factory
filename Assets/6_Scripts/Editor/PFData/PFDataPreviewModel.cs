using System;
using System.Collections.Generic;
using UnityEngine;

namespace PulseFactory.EditorTools
{
    internal sealed class PFDataPreviewModel : IDisposable
    {
        internal readonly struct Part
        {
            public readonly Mesh Mesh;
            public readonly Matrix4x4 Matrix;
            public readonly Material[] Materials;
            public Part(Mesh mesh, Matrix4x4 matrix, Material[] materials)
            { Mesh = mesh; Matrix = matrix; Materials = materials; }
        }

        public readonly List<Part> Parts = new();
        public readonly List<Mesh> Guides = new();
        private readonly List<Mesh> owned = new();
        public Bounds VisualBounds { get; private set; }
        public Bounds FrameBounds { get; private set; }
        public string Warning { get; private set; } = "";

        public PFDataPreviewModel(BuildingDefinitionSO definition)
        {
            try { Build(definition); }
            catch { Dispose(); throw; }
        }

        private void Build(BuildingDefinitionSO definition)
        {
            var prefab = definition.Prefab;
            var root = prefab.transform;
            var size = definition.PlacementSize;
            if (size.x <= 0f || size.y <= 0f)
            { Warning = "설치 면적이 올바르지 않아 1m 기준으로 표시합니다."; size = Vector2.one; }
            FrameBounds = new Bounds(Vector3.zero, new Vector3(size.x, 0f, size.y));
            var scale = root.localScale;
            if (Mathf.Abs(scale.x) < 0.000001f || Mathf.Abs(scale.y) < 0.000001f || Mathf.Abs(scale.z) < 0.000001f)
            { Warning = "Prefab 루트의 Scale이 0이어서 모델을 표시할 수 없습니다."; return; }
            var excluded = OtherLods(prefab);
            // Placement replaces root position/rotation, preserving its scale.
            var placement = Matrix4x4.Scale(scale) * root.worldToLocalMatrix;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !Active(renderer.transform) || excluded.Contains(renderer)) continue;
                Mesh mesh = null;
                if (renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
                else if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh != null)
                {
                    mesh = Own(new Mesh());
                    // Compensate renderer scale; the placement matrix applies it once.
                    skin.BakeMesh(mesh, true);
                }
                if (mesh == null || mesh.vertexCount == 0) continue;
                var matrix = placement * renderer.localToWorldMatrix;
                var bounds = TransformBounds(mesh.bounds, matrix);
                if (Parts.Count == 0) VisualBounds = bounds;
                else { var visual = VisualBounds; visual.Encapsulate(bounds); VisualBounds = visual; }
                var frame = FrameBounds; frame.Encapsulate(bounds); FrameBounds = frame;
                Parts.Add(new Part(mesh, matrix, renderer.sharedMaterials));
            }
            if (Parts.Count == 0) Warning = "표시 가능한 활성 MeshRenderer가 없습니다.";
            else if (VisualBounds.min.y < -0.01f) Warning = "모델 일부가 설치 바닥(y=0) 아래에 있습니다.";
            else if (VisualBounds.min.y > 0.01f) Warning = "모델이 설치 바닥(y=0)에서 떠 있습니다.";
            CreateGuides(size);
        }

        private static bool Active(Transform value)
        {
            for (; value != null; value = value.parent) if (!value.gameObject.activeSelf) return false;
            return true;
        }

        private static HashSet<Renderer> OtherLods(GameObject prefab)
        {
            var excluded = new HashSet<Renderer>();
            var first = new HashSet<Renderer>();
            foreach (var group in prefab.GetComponentsInChildren<LODGroup>(true))
            {
                if (!group.enabled || !Active(group.transform)) continue;
                var levels = group.GetLODs();
                for (int i = 0; i < levels.Length; i++)
                    foreach (var renderer in levels[i].renderers)
                        if (renderer != null) { if (i == 0) first.Add(renderer); else excluded.Add(renderer); }
            }
            excluded.ExceptWith(first);
            return excluded;
        }

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            var extents = bounds.extents;
            var x = matrix.MultiplyVector(Vector3.right * extents.x);
            var y = matrix.MultiplyVector(Vector3.up * extents.y);
            var z = matrix.MultiplyVector(Vector3.forward * extents.z);
            var size = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)) * 2f;
            return new Bounds(matrix.MultiplyPoint3x4(bounds.center), size);
        }

        private void CreateGuides(Vector2 size)
        {
            float span = Mathf.Max(1f, Mathf.Max(FrameBounds.max.magnitude, FrameBounds.min.magnitude));
            float step = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(span)));
            float half = Mathf.Ceil(span / step) * step;
            var plane = Own(new Mesh { name = "PF Preview Ground" });
            plane.vertices = new[] { new Vector3(-half, -0.005f, -half), new Vector3(-half, -0.005f, half),
                new Vector3(half, -0.005f, half), new Vector3(half, -0.005f, -half) };
            plane.triangles = new[] { 0, 1, 2, 0, 2, 3 }; plane.RecalculateNormals();
            Guides.Add(plane);
            var grid = new List<Vector3>();
            for (float v = -half; v <= half + step * 0.01f; v += step)
            {
                grid.Add(new Vector3(v, 0f, -half)); grid.Add(new Vector3(v, 0f, half));
                grid.Add(new Vector3(-half, 0f, v)); grid.Add(new Vector3(half, 0f, v));
            }
            Guides.Add(Lines(grid.ToArray()));
            float x = size.x * 0.5f, z = size.y * 0.5f, h = 0.008f;
            Guides.Add(Lines(new[] { new Vector3(-x,h,-z), new Vector3(-x,h,z), new Vector3(-x,h,z), new Vector3(x,h,z),
                new Vector3(x,h,z), new Vector3(x,h,-z), new Vector3(x,h,-z), new Vector3(-x,h,-z) }));
            Guides.Add(Lines(new[] { new Vector3(0,h,0), new Vector3(half,h,0) }));
            Guides.Add(Lines(new[] { new Vector3(0,h,0), new Vector3(0,h,half) }));
        }

        private Mesh Lines(Vector3[] vertices)
        {
            var mesh = Own(new Mesh { name = "PF Preview Guide" });
            var indices = new int[vertices.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            mesh.vertices = vertices; mesh.SetIndices(indices, MeshTopology.Lines, 0); mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh Own(Mesh mesh) { mesh.hideFlags = HideFlags.HideAndDontSave; owned.Add(mesh); return mesh; }
        public void Dispose()
        {
            foreach (var mesh in owned) if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            owned.Clear(); Parts.Clear(); Guides.Clear();
        }
    }
}

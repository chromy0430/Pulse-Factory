using System;
using UnityEditor;
using UnityEngine;

namespace PulseFactory.EditorTools
{
    internal sealed class PFDataPreviewRenderer : IDisposable
    {
        private PreviewRenderUtility preview;
        private readonly Material[] guideMaterials = new Material[5];
        private Material fallback;
        public PFDataPreviewModel Model { get; private set; }
        public Vector2 Orbit { get; private set; } = new Vector2(25f, -35f);
        public float Zoom { get; private set; } = 1f;
        public bool ShowGround { get; set; } = true;

        public void Select(BuildingDefinitionSO definition)
        {
            Dispose();
            Model = new PFDataPreviewModel(definition);
            Reset();
        }

        public void Reset() { Orbit = new Vector2(25f, -35f); Zoom = 1f; }
        public void View(bool top) { Orbit = top ? new Vector2(90f, 0f) : Vector2.zero; Zoom = 1f; }
        public void Rotate(Vector2 delta) => Orbit = new Vector2(Mathf.Clamp(Orbit.x + delta.y * 0.4f, -80f, 89f), Orbit.y + delta.x * 0.5f);
        public void Scale(float delta) => Zoom = Mathf.Clamp(Zoom * Mathf.Exp(delta * 0.08f), 0.15f, 8f);

        public Texture Render(Rect rect)
        {
            if (Model == null || rect.width < 2f || rect.height < 2f) return null;
            EnsureResources();
            preview.BeginPreview(rect, GUIStyle.none);
            Texture texture;
            try
            {
                var camera = preview.camera;
                float radius = Mathf.Max(0.1f, Model.FrameBounds.extents.magnitude);
                var rotation = Quaternion.Euler(Orbit.x, Orbit.y, 0f);
                camera.transform.SetPositionAndRotation(Model.FrameBounds.center - rotation * Vector3.forward * radius * 4f, rotation);
                camera.aspect = rect.width / rect.height;
                camera.orthographicSize = radius * 1.15f * Zoom / Mathf.Min(1f, camera.aspect);
                camera.nearClipPlane = 0.01f; camera.farClipPlane = Mathf.Max(20f, radius * 12f);
                foreach (var part in Model.Parts)
                    for (int i = 0; i < part.Mesh.subMeshCount; i++)
                    {
                        var material = part.Materials.Length == 0 ? null : part.Materials[Mathf.Min(i, part.Materials.Length - 1)];
                        preview.DrawMesh(part.Mesh, part.Matrix, material != null ? material : fallback, i);
                    }
                if (ShowGround)
                    for (int i = 0; i < Model.Guides.Count; i++) preview.DrawMesh(Model.Guides[i], Matrix4x4.identity, guideMaterials[i], 0);
                preview.Render(true, false);
            }
            finally { texture = preview.EndPreview(); }
            return texture;
        }

        private void EnsureResources()
        {
            if (preview != null) return;
            preview = new PreviewRenderUtility();
            preview.camera.orthographic = true;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.13f, 0.15f, 0.18f);
            preview.camera.allowHDR = false;
            preview.ambientColor = new Color(0.45f, 0.45f, 0.45f);
            var lights = preview.lights;
            lights[0].intensity = 1.3f; lights[0].transform.rotation = Quaternion.Euler(50f, -40f, 0f);
            lights[1].intensity = 0.7f; lights[1].transform.rotation = Quaternion.Euler(330f, 140f, 0f);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Hidden/Internal-Colored");
            var colors = new[] { new Color(0.20f,0.23f,0.27f), new Color(0.32f,0.36f,0.40f),
                new Color(1f,0.73f,0.22f), new Color(0.95f,0.3f,0.3f), new Color(0.3f,0.6f,1f) };
            for (int i = 0; i < colors.Length; i++)
                guideMaterials[i] = new Material(shader) { name = "PF Preview Guide " + i, color = colors[i], hideFlags = HideFlags.HideAndDontSave };
            fallback = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? shader)
                { name = "PF Preview Fallback", color = Color.gray, hideFlags = HideFlags.HideAndDontSave };
        }

        public void Dispose()
        {
            preview?.Cleanup(); preview = null;
            Model?.Dispose(); Model = null;
            foreach (var material in guideMaterials) if (material != null) UnityEngine.Object.DestroyImmediate(material);
            Array.Clear(guideMaterials, 0, guideMaterials.Length);
            if (fallback != null) UnityEngine.Object.DestroyImmediate(fallback);
            fallback = null;
        }
    }
}

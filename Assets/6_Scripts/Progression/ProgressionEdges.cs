using UnityEngine;

namespace PulseFactory.Progression
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ProgressionEdges : UnityEngine.UI.MaskableGraphic
    {
        public ProgressionCatalogSO catalog;
        public ProgressionKind kind;
        public Vector2 origin;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            if (catalog == null) return;
            foreach (var node in catalog.nodes)
            {
                if (node == null || node.kind != kind) continue;
                foreach (var parent in node.prerequisites)
                {
                    if (parent == null || parent.kind != kind || !catalog.Contains(parent)) continue;
                    Vector2 a = parent.position - origin + new Vector2(80, 44);
                    Vector2 b = node.position - origin + new Vector2(80, 44);
                    a.y = -a.y; b.y = -b.y;
                    Line(vh, a, b);
                    Vector2 direction = (b - a).normalized, tip = Vector2.Lerp(a, b, .65f);
                    Vector2 side = new(-direction.y, direction.x);
                    Line(vh, tip - direction * 12 + side * 6, tip);
                    Line(vh, tip, tip - direction * 12 - side * 6);
                }
            }
        }
        private void Line(UnityEngine.UI.VertexHelper vh, Vector2 a, Vector2 b)
        {
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * 1.5f;
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero); vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero); vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}

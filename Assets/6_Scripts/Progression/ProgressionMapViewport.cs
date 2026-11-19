using UnityEngine;
using UnityEngine.UI;

namespace PulseFactory.Progression
{
    public sealed class ProgressionMapViewport
    {
        public static readonly Vector2 NodeSize = new(160, 156);
        private readonly ProgressionMapScroll scroll;
        public Vector2 Origin { get; private set; }
        public float Zoom { get; set; } = 1;
        public ProgressionMapViewport(ProgressionMapScroll scroll) => this.scroll = scroll;
        public Vector2 Center
        {
            get
            {
                Vector2 local = scroll.content.InverseTransformPoint(scroll.viewport.TransformPoint(scroll.viewport.rect.center));
                return new Vector2(local.x, -local.y) + Origin;
            }
        }
        public void Arrange(ProgressionCatalogSO catalog, ProgressionKind kind)
        {
            Vector2 min = Vector2.zero, max = NodeSize;
            bool first = true;
            if (catalog != null) foreach (var node in catalog.nodes)
            {
                if (node == null || node.kind != kind) continue;
                if (first) { min = node.position; max = min + NodeSize; first = false; }
                min = Vector2.Min(min, node.position); max = Vector2.Max(max, node.position + NodeSize);
            }
            // Padding lets even the outermost node reach the viewport center at every zoom.
            Vector2 padding = scroll.viewport.rect.size / (2 * Zoom) + Vector2.one * 24;
            Origin = min - padding;
            scroll.content.sizeDelta = max - min + padding * 2;
            scroll.content.localScale = Vector3.one * Zoom;
        }
        public void CenterOn(Vector2 position)
        {
            Vector2 offset = (position - Origin) * Zoom;
            Vector2 half = scroll.viewport.rect.size * .5f;
            scroll.CancelCoast();
            scroll.content.anchoredPosition = new Vector2(half.x - offset.x, offset.y - half.y);
        }
    }
}

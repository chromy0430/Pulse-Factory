using UnityEngine;
using UnityEngine.UI;

namespace PulseFactory.Progression
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ProgressionLockGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Box(vh, .2f, .12f, .6f, .48f);
            Box(vh, .28f, .57f, .12f, .23f);
            Box(vh, .6f, .57f, .12f, .23f);
            Box(vh, .36f, .77f, .28f, .12f);
        }
        private void Box(VertexHelper vh, float x, float y, float w, float h)
        {
            Rect r = rectTransform.rect;
            Vector2 a = r.min + Vector2.Scale(new Vector2(x, y), r.size);
            Vector2 b = a + Vector2.Scale(new Vector2(w, h), r.size);
            int i = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero); vh.AddVert(new Vector2(a.x, b.y), color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero); vh.AddVert(new Vector2(b.x, a.y), color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}

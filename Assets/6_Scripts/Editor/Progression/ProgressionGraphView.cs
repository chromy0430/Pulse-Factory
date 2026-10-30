using System;
using System.Collections.Generic;
using PulseFactory.Progression;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactory.EditorTools
{
    public sealed class ProgressionGraphView : VisualElement
    {
        public Action<ProgressionNodeSO> Selected;
        public ProgressionCatalogSO Catalog;
        public ProgressionKind Kind;
        private readonly Dictionary<ProgressionNodeSO, VisualElement> cards = new();
        private Vector2 pan = new(120, 100);
        private float zoom = 1;
        private bool panning;
        private Vector2 last;
        private ProgressionNodeSO dragging;
        private VisualElement dragCard;
        private int pointerId;
        public Vector2 NewPosition => (contentRect.center - pan) / zoom;
        public ProgressionGraphView()
        {
            name = "progressionGraph";
            style.flexGrow = 1;
            style.overflow = Overflow.Hidden;
            style.backgroundColor = new Color(.075f, .085f, .11f);
            generateVisualContent += DrawEdges;
            RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 2 && !(e.button == 0 && e.target == this)) return;
                panning = true; last = e.position; pointerId = e.pointerId;
                this.CapturePointer(pointerId); e.StopPropagation();
            });
            RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!panning) return;
                pan += (Vector2)e.position - last; last = e.position; PositionCards();
            });
            RegisterCallback<PointerUpEvent>(e => { if (panning) { panning = false; this.ReleasePointer(pointerId); } });
            RegisterCallback<PointerCaptureOutEvent>(_ => panning = false);
            RegisterCallback<WheelEvent>(e =>
            {
                if (dragging != null) return;
                Vector2 point = this.WorldToLocal(e.mousePosition);
                Vector2 logical = (point - pan) / zoom;
                zoom = Mathf.Clamp(zoom * Mathf.Exp(-e.delta.y * .035f), .3f, 2);
                pan = point - logical * zoom; PositionCards(); e.StopPropagation();
            });
        }
        public void ResetView() { pan = new Vector2(120, 100); zoom = 1; PositionCards(); }
        public void Refresh()
        {
            if (dragging != null) return;
            Clear(); cards.Clear();
            if (Catalog == null) return;
            foreach (var node in Catalog.nodes)
            {
                if (node == null || node.kind != Kind) continue;
                var card = new VisualElement { name = "node-" + node.id, userData = node };
                card.style.position = Position.Absolute;
                card.style.backgroundColor = Catalog.Validate(node) == null ? new Color(.16f, .25f, .31f) : new Color(.45f, .15f, .15f);
                var icon = new Image { sprite = node.icon, pickingMode = PickingMode.Ignore };
                icon.style.height = 38;
                card.Add(icon);
                var label = new Label((node.entryNode ? "[시작] " : "") + node.title) { pickingMode = PickingMode.Ignore };
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.color = Color.white; card.Add(label);
                card.tooltip = node.description + "\n" + (Catalog.Validate(node) ?? "드래그: 위치 변경 / 선택 후 연결 버튼: 선행 조건 지정");
                card.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (e.button != 0) return;
                    Selected?.Invoke(node);
                    Undo.RegisterCompleteObjectUndo(node, "Move progression node");
                    dragging = node; dragCard = card; last = e.position; pointerId = e.pointerId;
                    card.CapturePointer(pointerId); card.BringToFront(); e.StopPropagation();
                });
                card.RegisterCallback<PointerMoveEvent>(e =>
                {
                    if (dragging != node) return;
                    node.position += ((Vector2)e.position - last) / zoom; last = e.position;
                    EditorUtility.SetDirty(node); PositionCards(); e.StopPropagation();
                });
                card.RegisterCallback<PointerUpEvent>(e => { if (dragging == node) EndDrag(); e.StopPropagation(); });
                card.RegisterCallback<PointerCaptureOutEvent>(_ => { if (dragging == node) EndDrag(); });
                cards[node] = card; Add(card);
            }
            PositionCards();
        }
        private void EndDrag()
        {
            var node = dragging; var card = dragCard; dragging = null; dragCard = null;
            if (card != null && card.HasPointerCapture(pointerId)) card.ReleasePointer(pointerId);
            if (node != null) ProgressionCatalogSync.Save(node);
        }
        private void PositionCards()
        {
            foreach (var pair in cards)
            {
                Vector2 p = pair.Key.position * zoom + pan;
                pair.Value.style.translate = new Translate(p.x, p.y);
                pair.Value.style.width = 160 * zoom; pair.Value.style.height = 88 * zoom;
            }
            MarkDirtyRepaint();
        }
        private void DrawEdges(MeshGenerationContext context)
        {
            if (Catalog == null || contentRect.width < 1) return;
            var painter = context.painter2D;
            painter.lineWidth = 2; painter.strokeColor = new Color(.35f, .7f, .8f);
            foreach (var node in Catalog.nodes)
            {
                if (node == null || !cards.ContainsKey(node)) continue;
                foreach (var parent in node.prerequisites)
                {
                    if (parent == null || !cards.ContainsKey(parent)) continue;
                    Vector2 a = (parent.position + new Vector2(80, 44)) * zoom + pan;
                    Vector2 b = (node.position + new Vector2(80, 44)) * zoom + pan;
                    painter.BeginPath(); painter.MoveTo(a); painter.LineTo(b); painter.Stroke();
                    Vector2 direction = (b - a).normalized, tip = Vector2.Lerp(a, b, .65f);
                    Vector2 side = new(-direction.y, direction.x);
                    painter.BeginPath(); painter.MoveTo(tip - direction * 10 + side * 5);
                    painter.LineTo(tip); painter.LineTo(tip - direction * 10 - side * 5); painter.Stroke();
                }
            }
        }
    }
}

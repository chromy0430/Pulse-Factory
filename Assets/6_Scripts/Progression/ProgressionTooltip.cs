using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PulseFactory.Progression
{
    public sealed class ProgressionTooltip : MonoBehaviour
    {
        private RectTransform bounds, box, textRect;
        private TextMeshProUGUI label;
        private ScrollRect scroll;
        private ProgressionNodeSO node;
        private ProgressionState state;
        private Object owner;
        private Vector2 screenPosition;
        public bool Visible => gameObject.activeSelf;
        public bool HasOverflow => textRect != null && textRect.rect.height > scroll.viewport.rect.height + 1;
        public void Initialize(RectTransform viewport)
        {
            bounds = viewport; box = (RectTransform)transform;
            ProgressionUIFactory.Image(box, new Color(.045f, .055f, .08f)).raycastTarget = false;
            var mask = ProgressionUIFactory.Rect("TooltipTextViewport", box, Vector2.one * 12, Vector2.one);
            mask.gameObject.AddComponent<RectMask2D>();
            label = ProgressionUIFactory.Text("TooltipText", mask, "", Vector2.zero, Vector2.one, 17);
            label.alignment = TextAlignmentOptions.TopLeft;
            label.richText = false;
            textRect = label.rectTransform;
            scroll = mask.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = mask; scroll.content = textRect; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = false;
            Hide();
        }
        public void Show(Object source, ProgressionNodeSO value, ProgressionState progress, Vector2 screenPoint)
        {
            if (owner == source && Visible) { Place(screenPoint); return; }
            owner = source; node = value; state = progress; screenPosition = screenPoint;
            gameObject.SetActive(true); transform.SetAsLastSibling();
            Refresh(); scroll.verticalNormalizedPosition = 1;
        }
        public void Refresh()
        {
            if (!Visible || node == null || state == null) { Hide(); return; }
            var text = new StringBuilder();
            text.AppendLine(string.IsNullOrWhiteSpace(node.tooltipTitle) ? node.title : node.tooltipTitle);
            text.AppendLine($"{(node.kind == ProgressionKind.Research ? "연구" : "특성")} · 단계 {state.Level(node)}/{node.MaxLevel}");
            text.AppendLine(string.IsNullOrWhiteSpace(node.tooltipDescription) ? node.description : node.tooltipDescription);
            if (state.Level(node) < node.MaxLevel)
                text.AppendLine($"비용: {node.Cost(state.Level(node)):N0} 크레딧 / {node.Cost(state.Level(node), true):N0} 연구 포인트");
            if (node.prerequisites.Count > 0)
            {
                text.AppendLine("선행 조건 (모두 최대 단계):");
                foreach (var parent in node.prerequisites)
                    text.AppendLine(parent == null ? "• 누락된 데이터" : $"• {parent.title} {state.Level(parent)}/{parent.MaxLevel}");
            }
            state.CanPurchase(node, out string reason);
            text.Append(reason ?? "구매 가능합니다.");
            label.text = text.ToString();
            float width = Mathf.Min(380, bounds.rect.width - 16);
            float height = label.GetPreferredValues(label.text, width - 24, Mathf.Infinity).y;
            float visibleHeight = Mathf.Min(height, bounds.rect.height - 40);
            box.sizeDelta = new Vector2(width, visibleHeight + 24);
            scroll.viewport.sizeDelta = new Vector2(width - 24, visibleHeight);
            textRect.sizeDelta = new Vector2(width - 24, height);
            Place(screenPosition);
        }
        public void Move(Object source, Vector2 screenPoint) { if (owner == source && Visible) Place(screenPoint); }
        private void Place(Vector2 screenPoint)
        {
            screenPosition = screenPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, screenPoint, null, out Vector2 p);
            float x = p.x - bounds.rect.xMin + 18;
            float y = bounds.rect.yMax - p.y + 18;
            if (x + box.rect.width > bounds.rect.width - 8) x -= box.rect.width + 36;
            if (y + box.rect.height > bounds.rect.height - 8) y -= box.rect.height + 36;
            box.anchoredPosition = new Vector2(Mathf.Clamp(x, 8, bounds.rect.width - box.rect.width - 8),
                -Mathf.Clamp(y, 8, bounds.rect.height - box.rect.height - 8));
        }
        public bool Scroll(float delta)
        {
            if (!Visible || !HasOverflow) return false;
            float max = textRect.rect.height - scroll.viewport.rect.height;
            textRect.anchoredPosition = new Vector2(0, Mathf.Clamp(textRect.anchoredPosition.y - delta * 28, 0, max));
            return true;
        }
        public void Hide(Object source = null)
        {
            if (source != null && source != owner) return;
            owner = null; node = null; gameObject.SetActive(false);
        }
    }
}

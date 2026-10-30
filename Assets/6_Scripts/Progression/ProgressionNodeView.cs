using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PulseFactory.Progression
{
    public sealed class ProgressionNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerMoveHandler, ISelectHandler, IDeselectHandler, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        private ProgressionNodeSO node;
        private ProgressionState state;
        private ProgressionTooltip tooltip;
        private ScrollRect map;
        private GameObject icon, lockIcon;
        private bool dragging;
        public bool Locked => lockIcon != null && lockIcon.activeSelf;
        public void Initialize(ProgressionNodeSO value, ProgressionState progress, ProgressionTooltip tip, ScrollRect scroll)
        {
            node = value; state = progress; tooltip = tip; map = scroll;
            var label = GetComponentInChildren<TextMeshProUGUI>();
            label.rectTransform.anchoredPosition = new Vector2(6, -48);
            label.rectTransform.sizeDelta = new Vector2(148, 36); label.fontSize = 16;
            if (node.icon != null)
            {
                var rect = ProgressionUIFactory.Rect("Icon", transform, new Vector2(60, 4), new Vector2(40, 40));
                var image = ProgressionUIFactory.Image(rect, Color.white);
                image.sprite = node.icon; image.preserveAspect = true; image.raycastTarget = false;
                icon = rect.gameObject;
            }
            else icon = ProgressionUIFactory.Text("Placeholder", transform, "◇", new Vector2(65, 2), new Vector2(40, 40), 28).gameObject;
            var lockRect = ProgressionUIFactory.Rect("Locked", transform, new Vector2(60, 4), new Vector2(40, 40));
            var graphic = lockRect.gameObject.AddComponent<ProgressionLockGraphic>();
            graphic.raycastTarget = false; graphic.color = new Color(.78f, .8f, .85f);
            lockIcon = lockRect.gameObject; Refresh();
        }
        public void Refresh()
        {
            bool locked = !state.CanAccess(node, out _);
            lockIcon.SetActive(locked); icon.SetActive(!locked);
        }
        private void OnDisable() { if (tooltip != null) tooltip.Hide(this); dragging = false; }
        public void OnPointerEnter(PointerEventData e) { if (!dragging && !e.dragging) tooltip.Show(this, node, state, e.position); }
        public void OnPointerExit(PointerEventData e) => tooltip.Hide(this);
        public void OnPointerMove(PointerEventData e) { if (!dragging) tooltip.Move(this, e.position); }
        public void OnSelect(BaseEventData e)
        {
            if (e is PointerEventData) return;
            tooltip.Show(this, node, state, RectTransformUtility.WorldToScreenPoint(null, transform.position));
        }
        public void OnDeselect(BaseEventData e) => tooltip.Hide(this);
        public void OnInitializePotentialDrag(PointerEventData e) => map.OnInitializePotentialDrag(e);
        public void OnBeginDrag(PointerEventData e) { dragging = true; e.eligibleForClick = false; tooltip.Hide(this); map.OnBeginDrag(e); }
        public void OnDrag(PointerEventData e) => map.OnDrag(e);
        public void OnEndDrag(PointerEventData e) { map.OnEndDrag(e); dragging = false; }
        public void OnScroll(PointerEventData e) { if (!tooltip.Scroll(e.scrollDelta.y)) map.OnScroll(e); }
    }
}

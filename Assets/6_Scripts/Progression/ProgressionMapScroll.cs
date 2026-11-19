using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PulseFactory.Progression
{
    public sealed class ProgressionMapScroll : ScrollRect
    {
        private const float CoastDuration = .16f;
        private const float CoastDistance = 44f;
        private readonly List<RaycastResult> hits = new();
        private bool dragging, coasting, refreshHover;
        private float coastTime;
        private Vector2 releasePosition;
        public bool Panning => dragging || coasting;
        public ProgressionTooltip Tooltip { private get; set; }

        public override void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            CancelCoast();
            dragging = true;
            Tooltip?.Hide();
            base.OnBeginDrag(e);
        }

        public override void OnEndDrag(PointerEventData e)
        {
            base.OnEndDrag(e);
            if (!dragging || e.button != PointerEventData.InputButton.Left) return;
            dragging = false;
            coasting = true;
            coastTime = 0;
            releasePosition = content.anchoredPosition;
            velocity = Vector2.ClampMagnitude(velocity, CoastDistance / CoastDuration * 2);
        }

        public void CancelCoast()
        {
            coasting = false;
            StopMovement();
            refreshHover = true;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (coasting)
            {
                coastTime += Time.unscaledDeltaTime;
                var delta = content.anchoredPosition - releasePosition;
                if (delta.sqrMagnitude >= CoastDistance * CoastDistance)
                {
                    content.anchoredPosition = releasePosition + delta.normalized * CoastDistance;
                    CancelCoast();
                }
                else if (coastTime >= CoastDuration || velocity.sqrMagnitude <= 4f)
                    CancelCoast();
            }
            if (!refreshHover || Panning) return;
            refreshHover = false;
            RefreshHoverAtPointer();
        }

        private void RefreshHoverAtPointer()
        {
            var events = EventSystem.current;
            var mouse = Mouse.current;
            if (!events || mouse == null || !Tooltip) return;
            var pointer = new PointerEventData(events) { position = mouse.position.ReadValue() };
            hits.Clear();
            events.RaycastAll(pointer, hits);
            var card = hits.Count > 0 ? hits[0].gameObject.GetComponentInParent<ProgressionNodeView>() : null;
            if (card && card.transform.IsChildOf(content)) card.Hover(pointer.position);
            else Tooltip.Hide();
            hits.Clear();
        }

        protected override void OnDisable()
        {
            dragging = coasting = refreshHover = false;
            base.OnDisable();
        }
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PulseFactory.Progression
{
    public sealed class ProgressionUI : MonoBehaviour
    {
        public ProgressionState state;
        public TMP_FontAsset font;
        public static bool IsOpen { get; private set; }
        private RectTransform canvasRoot, overlay, content;
        private UnityEngine.UI.ScrollRect scroll;
        private TextMeshProUGUI balance, detail;
        private UnityEngine.UI.Button purchase;
        private ProgressionKind kind;
        private ProgressionNodeSO selected;
        private readonly Dictionary<ProgressionNodeSO, ProgressionNodeView> cards = new();
        private ProgressionMapViewport map;
        private ProgressionTooltip tooltip;
        private Vector2 viewportSize;
        private int revision = -1;
        private bool dirty = true;
        private void Start()
        {
            if (state == null) state = GetComponent<ProgressionState>();
            if (state == null) { enabled = false; return; }
            state.Changed += MarkDirty;
            CreateUI();
        }
        private void OnDestroy()
        {
            if (state != null) state.Changed -= MarkDirty;
            if (canvasRoot != null) Destroy(canvasRoot.gameObject);
            IsOpen = false;
        }
        private void OnDisable() => Close();
        private void MarkDirty() => dirty = true;
        private void Update()
        {
            if (overlay == null) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame) Open(ProgressionKind.Research);
                if (keyboard.tKey.wasPressedThisFrame) Open(ProgressionKind.Trait);
                if (keyboard.escapeKey.wasPressedThisFrame) Close();
            }
            if (!IsOpen) return;
            if (revision != ProgressionNodeSO.Revision || viewportSize != scroll.viewport.rect.size) Rebuild();
            if (dirty) RefreshState();
        }
        private void CreateUI()
        {
            canvasRoot = ProgressionUIFactory.Rect("ProgressionCanvas", transform, Vector2.zero, Vector2.zero);
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = canvasRoot.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            canvasRoot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            ProgressionUIFactory.Button("Research", canvasRoot, "연구 [R]", new Vector2(20, 70), new Vector2(120, 40), () => Open(ProgressionKind.Research));
            ProgressionUIFactory.Button("Traits", canvasRoot, "특성 [T]", new Vector2(150, 70), new Vector2(120, 40), () => Open(ProgressionKind.Trait));
            overlay = ProgressionUIFactory.Rect("TreeOverlay", canvasRoot, Vector2.zero, Vector2.zero);
            ProgressionUIFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ProgressionUIFactory.Image(overlay, new Color(.035f, .045f, .07f, 1));
            ProgressionUIFactory.Button("ResearchTab", overlay, "연구", new Vector2(20, 20), new Vector2(110, 40), () => Open(ProgressionKind.Research));
            ProgressionUIFactory.Button("TraitTab", overlay, "특성", new Vector2(140, 20), new Vector2(110, 40), () => Open(ProgressionKind.Trait));
            ProgressionUIFactory.Button("ZoomOut", overlay, "−", new Vector2(270, 20), new Vector2(40, 40), () => SetZoom(map.Zoom / 1.2f));
            ProgressionUIFactory.Button("ZoomIn", overlay, "+", new Vector2(320, 20), new Vector2(40, 40), () => SetZoom(map.Zoom * 1.2f));
            ProgressionUIFactory.Button("Close", overlay, "닫기 [ESC]", new Vector2(380, 20), new Vector2(140, 40), Close);
            balance = ProgressionUIFactory.Text("Balance", overlay, "", new Vector2(550, 20), new Vector2(700, 40));
            var viewport = ProgressionUIFactory.Rect("Viewport", overlay, Vector2.zero, Vector2.zero);
            ProgressionUIFactory.Stretch(viewport, Vector2.zero, Vector2.one, new Vector2(20, 120), new Vector2(-20, -120));
            ProgressionUIFactory.Image(viewport, new Color(.07f, .09f, .12f));
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            scroll = viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewport; scroll.horizontal = scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            content = ProgressionUIFactory.Rect("Content", viewport, Vector2.zero, new Vector2(2000, 1200));
            scroll.content = content;
            map = new ProgressionMapViewport(scroll);
            var tooltipRect = ProgressionUIFactory.Rect("NodeTooltip", viewport, Vector2.zero, Vector2.one);
            tooltip = tooltipRect.gameObject.AddComponent<ProgressionTooltip>(); tooltip.Initialize(viewport);
            scroll.onValueChanged.AddListener(_ => tooltip.Hide());
            detail = ProgressionUIFactory.Text("Detail", overlay, "노드를 선택하세요. 빈 곳 드래그로 지도를 이동할 수 있습니다.", Vector2.zero, Vector2.zero);
            ProgressionUIFactory.Stretch(detail.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(20, 12), new Vector2(-220, 105));
            detail.fontSize = 16;
            purchase = ProgressionUIFactory.Button("Purchase", overlay, "구매 / 강화", Vector2.zero, new Vector2(180, 50), Purchase);
            var pr = (RectTransform)purchase.transform; pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(1, 0); pr.anchoredPosition = new Vector2(-20, 35);
            overlay.gameObject.SetActive(false);
        }
        public void Open(ProgressionKind value)
        {
            if (overlay == null) return;
            var build = FindFirstObjectByType<BuildController>();
            if (build != null) build.CancelMode();
            kind = value; selected = null; IsOpen = true; overlay.gameObject.SetActive(true); Rebuild();
            var entry = state.catalog != null ? state.catalog.Entry(kind) : null;
            map.CenterOn((entry != null ? entry.position : Vector2.zero) + ProgressionMapViewport.NodeSize * .5f);
        }
        public void Close()
        {
            IsOpen = false;
            if (tooltip != null) tooltip.Hide();
            if (overlay != null) overlay.gameObject.SetActive(false);
        }
        public void SetZoom(float value)
        {
            if (map == null) return;
            map.Zoom = Mathf.Clamp(value, .35f, 1.8f); Rebuild();
        }
        public void Rebuild()
        {
            if (content == null || state == null) return;
            tooltip.Hide();
            Canvas.ForceUpdateCanvases();
            Vector2 center = map.Center;
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            cards.Clear(); revision = ProgressionNodeSO.Revision;
            map.Arrange(state.catalog, kind);
            viewportSize = scroll.viewport.rect.size;
            Vector2 origin = map.Origin;
            var edgeRect = ProgressionUIFactory.Rect("Connections", content, Vector2.zero, content.sizeDelta);
            var edges = edgeRect.gameObject.AddComponent<ProgressionEdges>();
            edges.catalog = state.catalog; edges.kind = kind; edges.origin = origin; edges.raycastTarget = false;
            edges.color = new Color(.3f, .65f, .75f);
            if (state.catalog != null) foreach (var node in state.catalog.nodes)
            {
                if (node == null || node.kind != kind) continue;
                var button = ProgressionUIFactory.Button("Node-" + node.id, content, node.title,
                    node.position - origin, new Vector2(160, 88), () => { selected = node; RefreshState(); });
                var view = button.gameObject.AddComponent<ProgressionNodeView>();
                view.Initialize(node, state, tooltip, scroll); cards[node] = view;
            }
            if (selected != null && !cards.ContainsKey(selected)) selected = null;
            map.CenterOn(center);
            RefreshState();
        }
        public void RefreshState()
        {
            dirty = false;
            balance.text = $"크레딧 {CreditWallet.Instance?.CurrentCredit ?? 0:N0}   연구 포인트 {state.ResearchPoints:N0}";
            foreach (var pair in cards)
            {
                var node = pair.Key;
                bool available = state.CanPurchase(node, out _);
                pair.Value.Refresh();
                pair.Value.GetComponent<UnityEngine.UI.Image>().color = node == selected ? new Color(.55f, .42f, .12f) : state.Level(node) >= node.MaxLevel ? new Color(.13f, .45f, .32f) : available ? new Color(.13f, .32f, .47f) : new Color(.18f, .19f, .23f);
            }
            if (tooltip.Visible) tooltip.Refresh();
            purchase.interactable = selected != null && state.CanPurchase(selected, out _);
            if (selected == null) { detail.text = "드래그: 지도 이동 / + −: 확대·축소\n노드에 마우스: 상세 설명 / 긴 설명 위 휠: 설명 스크롤\n자물쇠: 선행 조건 미완료"; return; }
            state.CanPurchase(selected, out string reason);
            detail.text = $"{selected.title}  {state.Level(selected)}/{selected.MaxLevel}\n{selected.description}\n비용: {selected.Cost(state.Level(selected)):N0} 크레딧 / {selected.Cost(state.Level(selected), true):N0} 연구 포인트\n{reason ?? "구매 가능합니다."}";
        }
        public void Purchase()
        {
            if (selected != null && !state.TryPurchase(selected, out string reason)) detail.text = reason;
            else RefreshState();
        }
    }
}

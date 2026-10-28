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
        private readonly Dictionary<ProgressionNodeSO, UnityEngine.UI.Image> cards = new();
        private int revision = -1;
        private bool dirty = true;
        private float zoom = 1;
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
        private void OnDisable() { if (overlay != null) overlay.gameObject.SetActive(false); IsOpen = false; }
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
            if (revision != ProgressionNodeSO.Revision) Rebuild();
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
            ProgressionUIFactory.Image(overlay, new Color(.035f, .045f, .07f, .98f));
            ProgressionUIFactory.Button("ResearchTab", overlay, "연구", new Vector2(20, 20), new Vector2(110, 40), () => Open(ProgressionKind.Research));
            ProgressionUIFactory.Button("TraitTab", overlay, "특성", new Vector2(140, 20), new Vector2(110, 40), () => Open(ProgressionKind.Trait));
            ProgressionUIFactory.Button("ZoomOut", overlay, "−", new Vector2(270, 20), new Vector2(40, 40), () => SetZoom(zoom / 1.2f));
            ProgressionUIFactory.Button("ZoomIn", overlay, "+", new Vector2(320, 20), new Vector2(40, 40), () => SetZoom(zoom * 1.2f));
            ProgressionUIFactory.Button("Close", overlay, "닫기 [ESC]", new Vector2(380, 20), new Vector2(140, 40), Close);
            balance = ProgressionUIFactory.Text("Balance", overlay, "", new Vector2(550, 20), new Vector2(700, 40));
            var viewport = ProgressionUIFactory.Rect("Viewport", overlay, Vector2.zero, Vector2.zero);
            ProgressionUIFactory.Stretch(viewport, Vector2.zero, Vector2.one, new Vector2(20, 150), new Vector2(-20, -80));
            ProgressionUIFactory.Image(viewport, new Color(.07f, .09f, .12f));
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            scroll = viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewport; scroll.horizontal = scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            content = ProgressionUIFactory.Rect("Content", viewport, Vector2.zero, new Vector2(2000, 1200));
            scroll.content = content;
            detail = ProgressionUIFactory.Text("Detail", overlay, "노드를 선택하세요. 빈 곳 드래그로 지도를 이동할 수 있습니다.", Vector2.zero, Vector2.zero);
            ProgressionUIFactory.Stretch(detail.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(20, 20), new Vector2(-220, 135));
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
        }
        public void Close() { IsOpen = false; if (overlay != null) overlay.gameObject.SetActive(false); }
        public void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, .35f, 1.8f); content.localScale = Vector3.one * zoom;
            scroll.StopMovement();
        }
        public void Rebuild()
        {
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            cards.Clear(); revision = ProgressionNodeSO.Revision;
            Vector2 min = Vector2.zero, max = new(900, 500);
            if (state.catalog != null) foreach (var node in state.catalog.nodes)
            {
                if (node == null || node.kind != kind) continue;
                min = Vector2.Min(min, node.position); max = Vector2.Max(max, node.position + new Vector2(160, 88));
            }
            Vector2 origin = min - new Vector2(100, 100);
            content.sizeDelta = max - min + new Vector2(200, 200);
            var edgeRect = ProgressionUIFactory.Rect("Connections", content, Vector2.zero, content.sizeDelta);
            var edges = edgeRect.gameObject.AddComponent<ProgressionEdges>();
            edges.catalog = state.catalog; edges.kind = kind; edges.origin = origin; edges.raycastTarget = false;
            edges.color = new Color(.3f, .65f, .75f);
            if (state.catalog != null) foreach (var node in state.catalog.nodes)
            {
                if (node == null || node.kind != kind) continue;
                var button = ProgressionUIFactory.Button("Node-" + node.id, content, node.title,
                    node.position - origin, new Vector2(160, 88), () => { selected = node; RefreshState(); });
                cards[node] = button.GetComponent<UnityEngine.UI.Image>();
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                label.rectTransform.anchoredPosition = new Vector2(6, -48); label.rectTransform.sizeDelta = new Vector2(148, 36); label.fontSize = 16;
                if (node.icon != null)
                {
                    var icon = ProgressionUIFactory.Rect("Icon", button.transform, new Vector2(60, 4), new Vector2(40, 40));
                    var image = ProgressionUIFactory.Image(icon, Color.white); image.sprite = node.icon; image.preserveAspect = true; image.raycastTarget = false;
                }
                else ProgressionUIFactory.Text("Placeholder", button.transform, "◇", new Vector2(65, 2), new Vector2(40, 40), 28);
            }
            if (selected != null && !cards.ContainsKey(selected)) selected = null;
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
                pair.Value.color = node == selected ? new Color(.55f, .42f, .12f) : state.Level(node) >= node.MaxLevel ? new Color(.13f, .45f, .32f) : available ? new Color(.13f, .32f, .47f) : new Color(.18f, .19f, .23f);
            }
            purchase.interactable = selected != null && state.CanPurchase(selected, out _);
            if (selected == null) { detail.text = "노드를 선택하세요. 빈 곳 드래그: 지도 이동 / + −: 확대·축소"; return; }
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

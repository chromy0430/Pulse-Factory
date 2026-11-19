using System.Collections.Generic;
using TMPro;
using PulseFactory.UI;
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
        private UnityEngine.UI.Button purchase, researchTab, traitTab;
        private TextMeshProUGUI mapTitle, zoomLabel;
        public ProgressionKind CurrentKind => kind;
        public float Zoom => map?.Zoom ?? 1;
        public Vector2 MapCenter => map?.Center ?? Vector2.zero;
        public Vector2 MapViewportSize => scroll != null ? scroll.viewport.rect.size : Vector2.zero;
        public void CenterMap(Vector2 position) => map?.CenterOn(position);
        public void CenterEntry()
        {
            var entry=state?.catalog!=null?state.catalog.Entry(kind):null;
            CenterMap((entry!=null?entry.position:Vector2.zero)+ProgressionMapViewport.NodeSize*.5f);
        }
        private void FitEntry()
        {
            if (state.catalog == null) return;
            Canvas.ForceUpdateCanvases();
            var entry = state.catalog.Entry(kind);
            Vector2 center = (entry != null ? entry.position : Vector2.zero) + ProgressionMapViewport.NodeSize * .5f;
            Vector2 extent = ProgressionMapViewport.NodeSize;
            foreach (var node in state.catalog.nodes)
            {
                if (node == null || node.kind != kind) continue;
                var a = node.position - center; var b = node.position + ProgressionMapViewport.NodeSize - center;
                extent = Vector2.Max(extent, new Vector2(Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(b.x)), Mathf.Max(Mathf.Abs(a.y), Mathf.Abs(b.y))));
            }
            map.Zoom = Mathf.Clamp(Mathf.Min(scroll.viewport.rect.width / (2 * extent.x), scroll.viewport.rect.height / (2 * extent.y)) * .9f, .7f, 1f);
        }
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
            ProgressionUIFactory.Button("Research", canvasRoot, "연구 [R]", new Vector2(20, 100), new Vector2(120, 40), () => Open(ProgressionKind.Research));
            ProgressionUIFactory.Button("Traits", canvasRoot, "특성 [T]", new Vector2(150, 100), new Vector2(120, 40), () => Open(ProgressionKind.Trait));
            overlay = PFUI.Rect("TreeOverlay", canvasRoot, Vector2.zero, Vector2.zero);
            PFUI.Stretch(overlay, Vector2.zero, Vector2.one, new Vector2(10,78), new Vector2(-10,-108)); PFUI.Box(overlay);
            var titleIcon=PFUI.Icon("MapIcon",overlay,"Research",new Vector2(20,12),30);
            mapTitle=PFUI.Text("MapTitle",overlay,"연구 지도",new Vector2(64,8),new Vector2(260,40),27);
            var tabs=PFUI.Rect("Tabs",overlay,Vector2.zero,new Vector2(248,36));
            tabs.anchorMin=tabs.anchorMax=tabs.pivot=new Vector2(1,1);tabs.anchoredPosition=new Vector2(-76,-10);
            researchTab=PFUI.Button("ResearchTab",tabs,"연구",Vector2.zero,new Vector2(118,36),()=>Open(ProgressionKind.Research));
            traitTab=PFUI.Button("TraitTab",tabs,"특성",new Vector2(126,0),new Vector2(118,36),()=>Open(ProgressionKind.Trait));
            var close=PFUI.Button("Close",overlay,"",Vector2.zero,new Vector2(42,36),Close);
            var cr=(RectTransform)close.transform;cr.anchorMin=cr.anchorMax=cr.pivot=new Vector2(1,1);cr.anchoredPosition=new Vector2(-18,-10);
            PFUI.Icon("CloseIcon",cr,"Close",new Vector2(9,6),24);
            var viewport=PFUI.Rect("Viewport",overlay,Vector2.zero,Vector2.zero);
            PFUI.Stretch(viewport,Vector2.zero,Vector2.one,new Vector2(8,96),new Vector2(-8,-58));
            var grid=PFUI.Box(viewport,"Grid");grid.type=UnityEngine.UI.Image.Type.Tiled;
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            scroll=viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport=viewport;scroll.horizontal=scroll.vertical=true;
            scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=0;
            viewport.gameObject.AddComponent<PFMapZoom>().owner=this;
            content=PFUI.Rect("Content",viewport,Vector2.zero,new Vector2(2000,1200));scroll.content=content;
            map=new ProgressionMapViewport(scroll);
            var overview=PFUI.Rect("Overview",viewport,Vector2.zero,new Vector2(150,76));
            overview.anchorMin=overview.anchorMax=overview.pivot=new Vector2(1,0);overview.anchoredPosition=new Vector2(-10,10);
            overview.gameObject.AddComponent<PFMapOverview>().owner=this;
            var tooltipRect=PFUI.Rect("NodeTooltip",viewport,Vector2.zero,Vector2.one);
            tooltip=tooltipRect.gameObject.AddComponent<ProgressionTooltip>();tooltip.Initialize(viewport);
            scroll.onValueChanged.AddListener(_=>tooltip.Hide());
            detail=PFUI.Text("Detail",overlay,"노드를 선택하세요.",Vector2.zero,Vector2.zero,15);
            PFUI.Stretch(detail.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(20,20),new Vector2(-410,88));
            detail.textWrappingMode=TextWrappingModes.Normal;
            detail.maxVisibleLines=3;detail.overflowMode=TextOverflowModes.Ellipsis;
            var controls=PFUI.Rect("MapControls",overlay,Vector2.zero,new Vector2(172,70));
            controls.anchorMin=controls.anchorMax=controls.pivot=new Vector2(1,0);controls.anchoredPosition=new Vector2(-210,16);
            PFUI.Button("ZoomOut",controls,"−",Vector2.zero,new Vector2(36,30),()=>SetZoom(map.Zoom/1.2f));
            zoomLabel=PFUI.Text("Zoom",controls,"100%",new Vector2(42,0),new Vector2(66,30),17);zoomLabel.alignment=TextAlignmentOptions.Center;
            PFUI.Button("ZoomIn",controls,"+",new Vector2(114,0),new Vector2(36,30),()=>SetZoom(map.Zoom*1.2f));
            PFUI.Button("Recenter",controls,"시작점",new Vector2(0,36),new Vector2(150,30),CenterEntry,"Target");
            purchase=PFUI.Button("Purchase",overlay,"연구 / 강화",Vector2.zero,new Vector2(174,48),Purchase);
            var pr=(RectTransform)purchase.transform;pr.anchorMin=pr.anchorMax=pr.pivot=new Vector2(1,0);pr.anchoredPosition=new Vector2(-20,22);
            balance=PFUI.Text("Balance",overlay,"",Vector2.zero,Vector2.zero,12);
            balance.gameObject.SetActive(false);
            overlay.gameObject.SetActive(false);
        }

        public void Open(ProgressionKind value)
        {
            if (overlay == null) return;
            var build = FindFirstObjectByType<BuildController>();
            if (build != null) build.CancelMode();
            FindFirstObjectByType<BuildPanelView>()?.SetOpen(false);
            kind = value; mapTitle.text=kind==ProgressionKind.Research?"연구 지도":"특성 강화";
            PFUI.State(researchTab.GetComponent<UnityEngine.UI.Image>(),kind==ProgressionKind.Research);
            PFUI.State(traitTab.GetComponent<UnityEngine.UI.Image>(),kind==ProgressionKind.Trait);
            overlay.Find("MapIcon").GetComponent<UnityEngine.UI.Image>().sprite=PFUI.Theme?.Get(kind==ProgressionKind.Research?"Research":"Star");
            selected = null; IsOpen = true; overlay.gameObject.SetActive(true); FitEntry(); Rebuild();
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
            edges.catalog = state.catalog; edges.kind = kind; edges.origin = origin; edges.state=state; edges.raycastTarget = false;
            edges.color = new Color(.3f, .65f, .75f);
            if (state.catalog != null) foreach (var node in state.catalog.nodes)
            {
                if (node == null || node.kind != kind) continue;
                var button = ProgressionUIFactory.Button("Node-" + node.id, content, node.title,
                    node.position - origin, ProgressionMapViewport.NodeSize, () => { selected = node; RefreshState(); });
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
            zoomLabel.text=$"{Zoom*100:0}%";
            var edges=content.GetComponentInChildren<ProgressionEdges>();if(edges!=null)edges.SetVerticesDirty();
            balance.text = $"크레딧 {CreditWallet.Instance?.CurrentCredit ?? 0:N0}   연구 포인트 {state.ResearchPoints:N0}";
            foreach (var pair in cards)
            {
                var node = pair.Key;
                bool available = state.CanPurchase(node, out _);
                pair.Value.SetSelected(node == selected);
            }
            if (tooltip.Visible) tooltip.Refresh();
            purchase.interactable = selected != null && state.CanPurchase(selected, out _);
            if (selected == null) { detail.text = "완료 · 연구 가능 · 잠김\n드래그 이동 / 휠 확대·축소 / 노드 호버 상세 설명"; return; }
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

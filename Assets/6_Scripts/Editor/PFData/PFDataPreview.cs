using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactory.EditorTools
{
    internal sealed class PFDataPreview : VisualElement
    {
        private readonly PFDataPreviewRenderer renderer = new();
        private readonly Label assetName;
        private readonly Label dimensions;
        private readonly HelpBox message;
        private readonly IMGUIContainer canvas;
        private readonly VisualElement viewport;
        private BuildingDefinitionSO definition;
        private GameObject prefab;
        private Vector2 footprint;
        private Hash128 hash;
        private Vector2 last;
        private bool dragging;

        public PFDataPreview()
        {
            name = "pf-preview"; AddToClassList("pf-preview");
            var title = new Label("건설 미리보기"); title.AddToClassList("pf-heading"); Add(title);
            assetName = new Label { name = "pf-preview-prefab" }; assetName.AddToClassList("pf-path"); Add(assetName);
            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(() => { renderer.Reset(); canvas.MarkDirtyRepaint(); }) { text = "기본", name = "pf-preview-reset" });
            toolbar.Add(new ToolbarButton(() => { renderer.View(true); canvas.MarkDirtyRepaint(); }) { text = "위", name = "pf-preview-top" });
            toolbar.Add(new ToolbarButton(() => { renderer.View(false); canvas.MarkDirtyRepaint(); }) { text = "정면", name = "pf-preview-front" });
            var ground = new ToolbarToggle { text = "바닥·설치 영역", value = true, name = "pf-preview-ground" };
            ground.RegisterValueChangedCallback(evt => { renderer.ShowGround = evt.newValue; canvas.MarkDirtyRepaint(); });
            toolbar.Add(ground); Add(toolbar);
            viewport = new VisualElement { name = "pf-preview-viewport", focusable = true };
            canvas = new IMGUIContainer(Draw) { name = "pf-preview-canvas", pickingMode = PickingMode.Ignore };
            viewport.Add(canvas); Add(viewport);
            viewport.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || renderer.Model == null) return;
                dragging = true; last = evt.position; viewport.CapturePointer(evt.pointerId); evt.StopPropagation();
            });
            viewport.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!dragging || !viewport.HasPointerCapture(evt.pointerId)) return;
                Vector2 current = evt.position; renderer.Rotate(current - last); last = current;
                canvas.MarkDirtyRepaint(); evt.StopPropagation();
            });
            viewport.RegisterCallback<PointerUpEvent>(evt =>
            { if (!dragging) return; dragging = false; viewport.ReleasePointer(evt.pointerId); evt.StopPropagation(); });
            viewport.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
            viewport.RegisterCallback<WheelEvent>(evt =>
            { if (renderer.Model == null) return; renderer.Scale(evt.delta.y); canvas.MarkDirtyRepaint(); evt.StopPropagation(); });
            viewport.RegisterCallback<GeometryChangedEvent>(_ => canvas.MarkDirtyRepaint());
            dimensions = new Label { name = "pf-preview-dimensions" }; dimensions.AddToClassList("pf-preview-help"); Add(dimensions);
            message = new HelpBox("", HelpBoxMessageType.Info); Add(message);
            var help = new Label("드래그: 회전 · 휠: 확대/축소\n노란 테두리: SO 설치 면적 · 빨강 +X / 파랑 +Z\n바닥 y=0은 설치 피벗입니다.\n정적 모델 미리보기이며 파티클·게임 로직은 실행하지 않습니다.");
            help.AddToClassList("pf-preview-help"); Add(help);
            RegisterCallback<DetachFromPanelEvent>(_ => Release());
            RegisterCallback<AttachToPanelEvent>(_ => { if (definition != null) Select(definition); });
        }

        public void Select(ScriptableObject asset)
        {
            definition = asset as BuildingDefinitionSO;
            var nextPrefab = definition != null ? definition.Prefab : null;
            var nextFootprint = definition != null ? definition.PlacementSize : Vector2.zero;
            var nextHash = nextPrefab != null ? AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(nextPrefab)) : default;
            if (nextPrefab == prefab && nextFootprint == footprint && nextHash == hash && renderer.Model != null) return;
            prefab = nextPrefab; footprint = nextFootprint; hash = nextHash;
            renderer.Dispose();
            assetName.text = prefab != null ? prefab.name : "";
            assetName.tooltip = prefab != null ? AssetDatabase.GetAssetPath(prefab) : "";
            dimensions.text = "";
            if (definition == null) message.text = "건물 데이터를 선택하면 3D 미리보기를 표시합니다.";
            else if (prefab == null) message.text = "Prefab을 지정하면 3D 미리보기를 표시합니다.";
            else
            {
                try
                {
                    renderer.Select(definition);
                    var size = renderer.Model.VisualBounds.size;
                    dimensions.text = $"모델 X/Y/Z: {size.x:0.##} / {size.y:0.##} / {size.z:0.##} m\n설치 면적 X×Z: {footprint.x:0.##} × {footprint.y:0.##} m";
                    message.text = renderer.Model.Warning;
                }
                catch (System.Exception error) { Release(); message.text = "미리보기를 표시할 수 없습니다: " + error.Message; }
            }
            message.style.display = string.IsNullOrEmpty(message.text) ? DisplayStyle.None : DisplayStyle.Flex;
            canvas.MarkDirtyRepaint();
        }

        private void Draw()
        {
            if (Event.current.type != EventType.Repaint || renderer.Model == null) return;
            var rect = new Rect(0f, 0f, canvas.contentRect.width, canvas.contentRect.height);
            try
            {
                var texture = renderer.Render(rect);
                if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
            }
            catch (System.Exception error)
            {
                Release(); message.text = "미리보기를 그릴 수 없습니다: " + error.Message;
                message.style.display = DisplayStyle.Flex;
            }
        }

        public void Release() { renderer.Dispose(); dragging = false; }
    }
}

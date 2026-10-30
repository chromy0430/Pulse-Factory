using PulseFactory.Progression;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactory.EditorTools
{
    public sealed class ProgressionWindow : EditorWindow
    {
        [SerializeField] private ProgressionNodeSO selected;
        [SerializeField] private ProgressionKind kind;
        private ProgressionNodeSO linkSource;
        private ProgressionCatalogSO catalog;
        private ProgressionGraphView graph;
        private ScrollView inspector;
        private Label status;
        private int revision;
        [MenuItem("Tools/PF Progression")]
        public static void Open() => GetWindow<ProgressionWindow>("PF Progression");
        private void OnEnable() { Undo.undoRedoPerformed += UndoChanged; EditorApplication.update += Tick; }
        private void OnDisable() { Undo.undoRedoPerformed -= UndoChanged; EditorApplication.update -= Tick; }
        private void UndoChanged() { ProgressionNodeSO.NotifyChanged(); ShowInspector(); }
        private void Tick()
        {
            if (graph == null || revision == ProgressionNodeSO.Revision) return;
            revision = ProgressionNodeSO.Revision;
            graph.Refresh();
        }
        public void CreateGUI()
        {
            minSize = new Vector2(850, 450);
            catalog = ProgressionCatalogSync.Synchronize();
            var bar = new Toolbar();
            bar.Add(new ToolbarButton(() => Switch(ProgressionKind.Research)) { text = "연구" });
            bar.Add(new ToolbarButton(() => Switch(ProgressionKind.Trait)) { text = "특성" });
            bar.Add(new ToolbarButton(() => Select(ProgressionCatalogSync.Create(kind, graph.NewPosition))) { text = "+ 노드 추가" });
            bar.Add(new ToolbarButton(() => { linkSource = selected; status.text = selected == null ? "먼저 선행 노드를 선택하세요." : selected.title + " → 다음 노드를 클릭하세요. (해제: 빈 선택 후 연결 취소)"; }) { text = "선택 노드에서 연결" });
            bar.Add(new ToolbarButton(() => { linkSource = null; status.text = "연결 취소"; }) { text = "연결 취소" });
            bar.Add(new ToolbarButton(Delete) { text = "선택 삭제" });
            bar.Add(new ToolbarButton(() => { AssetDatabase.SaveAssets(); ProgressionNodeSO.NotifyChanged(); }) { text = "저장" });
            bar.Add(new ToolbarButton(() => graph.ResetView()) { text = "뷰 초기화" });
            rootVisualElement.Add(bar);
            status = new Label("노드 드래그: 배치 / 빈 곳 또는 휠 버튼 드래그: 이동 / 휠: 확대·축소 / 변경 자동 반영");
            rootVisualElement.Add(status);
            var split = new TwoPaneSplitView(1, 320, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;
            graph = new ProgressionGraphView { Catalog = catalog, Kind = kind, Selected = Select };
            split.Add(graph);
            inspector = new ScrollView(); split.Add(inspector); rootVisualElement.Add(split);
            graph.Refresh(); ShowInspector();
        }
        private void Switch(ProgressionKind value)
        {
            kind = value; selected = null; linkSource = null; graph.Kind = kind;
            graph.Refresh(); ShowInspector();
        }
        private void Select(ProgressionNodeSO node)
        {
            if (linkSource != null)
            {
                bool ok = ProgressionCatalogSync.Link(linkSource, node, catalog, out string error);
                status.text = ok ? "연결했습니다." : error; linkSource = null;
            }
            selected = node; ShowInspector();
        }
        private void ShowInspector()
        {
            if (inspector == null) return;
            inspector.Clear();
            if (selected == null) { inspector.Add(new Label("노드를 선택하거나 추가하세요.")); return; }
            inspector.Add(new Label("Icon에 Sprite를 넣으면 에디터와 게임에 함께 표시됩니다.\nPosition과 Prerequisites는 게임 UI에도 적용됩니다.\n선행 노드는 최대 단계 완료가 필요합니다."));
            inspector.Add(new InspectorElement(selected));
            inspector.Add(new Button(() =>
            {
                foreach (var node in catalog.nodes)
                {
                    if (node == null || node.kind != selected.kind) continue;
                    Undo.RecordObject(node, "Set progression entry node");
                    node.entryNode = node == selected; ProgressionCatalogSync.Save(node);
                }
                status.text = "게임에서 이 트리를 열면 선택 노드가 화면 중앙에 표시됩니다.";
                ShowInspector();
            }) { text = "이 노드를 시작점으로 지정" });
            inspector.Add(new HelpBox("Entry Node: 처음 중앙에 놓을 노드\nTooltip Title / Tooltip Description: 호버 설명 (빈 값은 Title / Description 사용)\n자물쇠는 선행 조건 미완료를 나타내며, 재화 부족과 구분합니다.", HelpBoxMessageType.Info));
            inspector.Add(new Button(() =>
            {
                Undo.RecordObject(selected, "Clear prerequisites"); selected.prerequisites.Clear();
                ProgressionCatalogSync.Save(selected); ShowInspector();
            }) { text = "이 노드의 선행 연결 모두 해제" });
        }
        private void Delete()
        {
            if (selected == null || EditorApplication.isPlaying) { status.text = "삭제는 Edit Mode에서 가능합니다."; return; }
            string path = AssetDatabase.GetAssetPath(selected);
            if (!EditorUtility.DisplayDialog("연구/특성 삭제", path + "\n이 데이터를 휴지통으로 이동하고 다른 노드의 선행 연결에서 제거합니다. 복원은 운영체제 휴지통을 사용하세요.", "휴지통으로 이동", "취소")) return;
            var target = selected;
            if (!AssetDatabase.MoveAssetToTrash(path)) { status.text = "삭제하지 못했습니다."; return; }
            foreach (var node in catalog.nodes)
            {
                if (node == null || !node.prerequisites.Exists(p => p == null || p == target)) continue;
                Undo.RecordObject(node, "Remove deleted prerequisite");
                node.prerequisites.RemoveAll(p => p == null || p == target); ProgressionCatalogSync.Save(node);
            }
            selected = null; linkSource = null;
            catalog = ProgressionCatalogSync.Synchronize(); graph.Catalog = catalog;
            graph.Refresh(); ShowInspector();
        }
    }
}

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactory.EditorTools
{
    internal sealed class PFDataInspector : VisualElement
    {
        private UnityEditor.Editor editor;
        private ScriptableObject asset;
        private readonly Label title;
        private readonly Label path;
        private readonly IMGUIContainer inspector;
        private System.Collections.Generic.List<ScriptableObject> buildings;
        private readonly System.Action changed;

        public PFDataInspector(System.Action changed)
        {
            this.changed = changed;
            AddToClassList("pf-detail");
            title = new Label("왼쪽에서 데이터를 선택하세요.") { name = "pf-detail-title" };
            title.AddToClassList("pf-heading");
            Add(title);
            var actions = new VisualElement();
            actions.AddToClassList("pf-row");
            actions.Add(new Button(() => { if (asset != null) AssetDatabase.SaveAssetIfDirty(asset); changed?.Invoke(); })
                { text = "선택 데이터 저장", name = "pf-save", tooltip = "선택한 데이터의 변경 사항을 디스크에 저장합니다. 참조하는 다른 데이터는 해당 탭에서 별도로 저장하세요." });
            actions.Add(new Button(() => { if (asset != null) EditorGUIUtility.PingObject(asset); })
                { text = "Project에서 찾기", name = "pf-ping" });
            Add(actions);
            path = new Label { name = "pf-detail-path" };
            path.AddToClassList("pf-path");
            Add(path);
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            inspector = new IMGUIContainer(DrawInspector) { name = "pf-inspector" };
            scroll.Add(inspector);
            Add(scroll);
            Add(new HelpBox("필드 편집은 즉시 에셋에 반영되며 Ctrl+Z로 되돌릴 수 있습니다. 저장 버튼으로 디스크에 저장하세요. 건설 메뉴 표시를 켠 건물은 게임 시작 시 B키 목록에 자동 등록됩니다. Prefab과 Icon을 지정하세요.", HelpBoxMessageType.Info));
        }

        public void Select(ScriptableObject value)
        {
            buildings = PFDataCatalog.Find(0);
            title.text = value == null ? "왼쪽에서 데이터를 선택하거나 새로 만드세요." : "Editing: " + value.name + (EditorUtility.IsDirty(value) ? " *" : "");
            path.text = value == null ? "" : AssetDatabase.GetAssetPath(value);
            if (asset == value && editor != null) return;
            Release();
            asset = value;
            if (asset != null) editor = UnityEditor.Editor.CreateEditor(asset);
            inspector.MarkDirtyRepaint();
        }

        public void Release()
        {
            if (editor != null) UnityEngine.Object.DestroyImmediate(editor);
            editor = null;
        }

        private void DrawInspector()
        {
            if (asset == null || editor == null) return;
            if (asset is BuildingDefinitionSO building)
            {
                if (string.IsNullOrWhiteSpace(building.Id))
                    EditorGUILayout.HelpBox("건물 ID를 입력하세요.", MessageType.Warning);
                else
                    foreach (var other in buildings)
                        if (other != null && other != asset && ((BuildingDefinitionSO)other).Id == building.Id)
                        {
                            EditorGUILayout.HelpBox("같은 ID를 사용하는 건물이 있습니다: " + other.name, MessageType.Warning);
                            break;
                        }
            }
            EditorGUI.BeginChangeCheck();
            editor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck()) changed?.Invoke();
        }
    }
}

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactory.EditorTools
{
    public sealed class PFDataWindow : EditorWindow
    {
        private const string StylesPath = "Assets/6_Scripts/Editor/PFData/PFDataWindow.uss";
        [SerializeField] private int category;
        [SerializeField] private ScriptableObject selected;
        [SerializeField] private string search = "";
        private ScrollView list;
        private VisualElement formHost;
        private Label count;
        private PFDataInspector detail;
        private PFDataPreview preview;
        private readonly Button[] tabs = new Button[3];

        [MenuItem("Tools/PF Data")]
        public static void Open()
        {
            var window = GetWindow<PFDataWindow>();
            window.titleContent = new GUIContent("PF Data");
            window.minSize = new Vector2(1180, 560);
            if (window.position.width < 1320)
            { var rect = window.position; rect.width = 1320; window.position = rect; }
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += Refresh;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Undo.undoRedoPerformed += Refresh;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= Refresh;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Undo.undoRedoPerformed -= Refresh;
            detail?.Release();
            preview?.Release();
        }

        public void CreateGUI()
        {
            detail?.Release();
            preview?.Release();
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("pf-data");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesPath);
            if (styleSheet != null) rootVisualElement.styleSheets.Add(styleSheet);
            var toolbar = new Toolbar();
            toolbar.Add(new Label("PF DATA") { name = "pf-brand" });
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i] = new ToolbarButton(() => ChangeCategory(index))
                    { text = PFDataCatalog.Categories[i], name = "pf-tab-" + i };
                toolbar.Add(tabs[i]);
            }
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);
            toolbar.Add(new ToolbarButton(Refresh) { text = "새로고침", name = "pf-refresh" });
            rootVisualElement.Add(toolbar);
            var split = new TwoPaneSplitView(0, 290, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "pf-data-split" };
            split.style.flexGrow = 1;
            var sidebar = new VisualElement();
            sidebar.AddToClassList("pf-sidebar");
            var filter = new ToolbarSearchField { name = "pf-search", value = search };
            filter.RegisterValueChangedCallback(evt => { search = evt.newValue; Refresh(); });
            sidebar.Add(filter);
            count = new Label { name = "pf-count" };
            sidebar.Add(count);
            list = new ScrollView(ScrollViewMode.Vertical) { name = "pf-list" };
            list.style.flexGrow = 1;
            sidebar.Add(list);
            formHost = new VisualElement { name = "pf-form-host" };
            sidebar.Add(formHost);
            var create = new Button(() => ShowCreate(null)) { text = "+ 새 데이터 만들기", name = "pf-new" };
            create.AddToClassList("pf-primary");
            sidebar.Add(create);
            sidebar.Add(new Button(() => { if (selected != null) ShowCreate(selected); })
                { text = "선택 데이터 복제", name = "pf-duplicate" });
            var delete = new Button(ShowDelete) { text = "선택 데이터 삭제", name = "pf-delete" };
            delete.AddToClassList("pf-danger");
            sidebar.Add(delete);
            split.Add(sidebar);
            detail = new PFDataInspector(() => rootVisualElement.schedule.Execute(Refresh));
            var content = new TwoPaneSplitView(1, 350, TwoPaneSplitViewOrientation.Horizontal)
                { viewDataKey = "pf-data-preview-split", name = "pf-data-content" };
            content.style.flexGrow = 1;
            content.Add(detail);
            preview = new PFDataPreview();
            content.Add(preview);
            split.Add(content);
            rootVisualElement.Add(split);
            Refresh();
        }

        private void ChangeCategory(int index)
        {
            category = index;
            selected = null;
            formHost.Clear();
            Refresh();
        }

        private void ShowCreate(ScriptableObject template)
        {
            formHost.Clear();
            formHost.Add(new PFDataCreateForm(category, template, asset =>
            {
                selected = asset;
                search = "";
                rootVisualElement.Q<ToolbarSearchField>("pf-search").SetValueWithoutNotify("");
                formHost.Clear();
                Refresh();
                EditorGUIUtility.PingObject(asset);
            }, () => formHost.Clear()));
        }

        private void OnPlayModeChanged(PlayModeStateChange state) { formHost?.Clear(); Refresh(); }

        private void ShowDelete()
        {
            if (selected == null) return;
            formHost.Clear();
            formHost.Add(new PFDataDeleteForm(selected, RemoveData, () => formHost.Clear()));
        }

        private void RemoveData(ScriptableObject asset)
        {
            string error = PFDataDeleteForm.Validate(asset);
            if (error != null) throw new InvalidOperationException(error);
            string path = AssetDatabase.GetAssetPath(asset);
            bool buildingDeleted = asset is BuildingDefinitionSO;
            detail.Release();
            preview?.Release();
            try
            {
                if (!AssetDatabase.MoveAssetToTrash(path))
                    throw new InvalidOperationException("휴지통으로 이동하지 못했습니다. 파일 권한과 버전 관리 상태를 확인하세요.");
                selected = null;
                formHost.Clear();
                AssetDatabase.Refresh();
                if (buildingDeleted) BuildMenuCatalogSync.Synchronize();
            }
            finally { Refresh(); }
        }

        private void Refresh()
        {
            if (list == null) return;
            category = Mathf.Clamp(category, 0, tabs.Length - 1);
            for (int i = 0; i < tabs.Length; i++) tabs[i].EnableInClassList("pf-active-tab", i == category);
            var all = PFDataCatalog.Find(category);
            var matches = PFDataCatalog.Find(category, search);
            if (selected == null || !all.Contains(selected))
            {
                formHost.Clear();
                selected = matches.Count > 0 ? matches[0] : null;
            }
            count.text = $"{PFDataCatalog.Categories[category]}  {matches.Count} / {all.Count}";
            list.Clear();
            foreach (Type type in PFDataCatalog.Types(category))
            {
                int groupCount = 0;
                foreach (var asset in matches) if (asset.GetType() == type) groupCount++;
                if (groupCount == 0) continue;
                var group = new Foldout { text = PFDataCatalog.TypeLabel(type) + " (" + groupCount + ")", value = true,
                    viewDataKey = "pf-group-" + type.FullName };
                foreach (var asset in matches)
                {
                    if (asset.GetType() != type) continue;
                    var row = new Button(() => { selected = asset; formHost.Clear(); Refresh(); })
                        { text = asset.name + (EditorUtility.IsDirty(asset) ? " *" : ""), tooltip = AssetDatabase.GetAssetPath(asset), userData = asset };
                    row.AddToClassList("pf-asset");
                    row.EnableInClassList("pf-selected", selected == asset);
                    group.Add(row);
                }
                list.Add(group);
            }
            if (matches.Count == 0) list.Add(new HelpBox("표시할 데이터가 없습니다. 검색어를 바꾸거나 새 데이터를 만드세요.", HelpBoxMessageType.Info));
            rootVisualElement.Q<Button>("pf-duplicate").SetEnabled(selected != null);
            var delete = rootVisualElement.Q<Button>("pf-delete");
            string deletionError = PFDataDeleteForm.Validate(selected);
            delete.SetEnabled(deletionError == null);
            delete.tooltip = deletionError ?? "선택한 .asset 파일을 휴지통으로 보냅니다. 삭제 전에 대상을 확인합니다.";
            detail.Select(selected);
            preview?.Select(selected);
        }
    }
}

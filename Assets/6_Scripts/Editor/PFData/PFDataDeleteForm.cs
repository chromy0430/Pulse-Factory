using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PulseFactory.EditorTools
{
    internal sealed class PFDataDeleteForm : VisualElement
    {
        private readonly ScriptableObject target;
        private readonly string path;
        private readonly string guid;
        private readonly HelpBox message;
        private readonly Button confirm;
        private string references;

        public PFDataDeleteForm(ScriptableObject asset, Action<ScriptableObject> remove, Action cancel)
        {
            name = "pf-delete-form";
            AddToClassList("pf-create-form");
            target = asset;
            path = AssetDatabase.GetAssetPath(asset);
            guid = AssetDatabase.AssetPathToGUID(path);
            Add(new Label("선택 데이터를 삭제할까요?") { name = "pf-delete-title" });
            Add(new Label(asset.name + "\n" + path) { name = "pf-delete-target" });
            Add(new Label("파일과 .meta를 휴지통으로 보냅니다. 복원은 운영체제 휴지통에서 할 수 있습니다. " +
                "저장하지 않은 변경은 복원되지 않으며 Ctrl+Z로는 취소할 수 없습니다.") { name = "pf-delete-hint" });
            var details = new ScrollView(ScrollViewMode.Vertical) { name = "pf-delete-details" };
            message = new HelpBox("", HelpBoxMessageType.Warning) { name = "pf-delete-warning" };
            details.Add(message);
            Add(details);
            var actions = new VisualElement();
            actions.AddToClassList("pf-row");
            actions.Add(new Button(cancel) { text = "취소", name = "pf-delete-cancel" });
            confirm = new Button(() =>
            {
                try
                {
                    string error = Validate(target);
                    if (error != null) throw new InvalidOperationException(error);
                    if (AssetDatabase.GetAssetPath(target) != path || AssetDatabase.AssetPathToGUID(path) != guid)
                        throw new InvalidOperationException("파일 위치나 대상이 바뀌었습니다. 취소 후 다시 선택하세요.");
                    string current = FindReferences(target);
                    if (current != references)
                    {
                        references = current;
                        ShowReferences("참조 상태가 바뀌었습니다. 내용을 확인한 뒤 다시 눌러 주세요.\n");
                        return;
                    }
                    remove(target);
                }
                catch (Exception ex)
                {
                    message.text = ex.Message;
                    message.messageType = HelpBoxMessageType.Error;
                }
            }) { text = "휴지통으로 이동", name = "pf-delete-confirm" };
            confirm.AddToClassList("pf-danger");
            actions.Add(confirm);
            Add(actions);
            string validation = Validate(asset);
            if (validation != null)
            {
                message.text = validation;
                confirm.SetEnabled(false);
                return;
            }
            references = FindReferences(asset);
            ShowReferences("");
        }

        public static string Validate(ScriptableObject asset)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "플레이 모드를 종료한 뒤 삭제하세요.";
            if (asset == null) return "삭제할 데이터를 선택하세요.";
            if (!(asset is BuildingDefinitionSO) && !(asset is PowerCableDefinitionSO) && !(asset is BuildingEffectSO))
                return "PF Data에서 관리하는 데이터만 삭제할 수 있습니다.";
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) || !AssetDatabase.IsMainAsset(asset))
                return "Assets 아래의 독립된 .asset 파일만 삭제할 수 있습니다.";
            if (AssetDatabase.LoadAllAssetsAtPath(assetPath).Length != 1)
                return "하위 에셋을 포함한 파일은 Project 창에서 내용을 확인한 뒤 관리하세요.";
            if (!AssetDatabase.IsOpenForEdit(assetPath)) return "읽기 전용이거나 버전 관리에서 잠긴 파일입니다.";
            return null;
        }

        private void ShowReferences(string prefix)
        {
            message.messageType = HelpBoxMessageType.Warning;
            message.text = prefix + (string.IsNullOrEmpty(references)
                ? "저장된 씬·Prefab·SO와 열린 씬에서 참조를 찾지 못했습니다. 자동 건설 목록에서도 제거됩니다. " +
                  "코드·문자열로 불러오는 참조는 검사하지 않습니다."
                : "다음 항목이 이 데이터를 참조합니다. 삭제하면 해당 참조가 끊어집니다.\n" + references +
                  "\n자동 건설 목록은 갱신됩니다. 코드·문자열로 불러오는 참조는 검사하지 않습니다.");
        }

        private static string FindReferences(ScriptableObject asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            var found = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string candidate in AssetDatabase.GetAllAssetPaths())
            {
                if (!candidate.StartsWith("Assets/", StringComparison.Ordinal) || candidate == assetPath ||
                    candidate == BuildMenuCatalogSync.CatalogPath) continue;
                string extension = Path.GetExtension(candidate).ToLowerInvariant();
                if (extension != ".asset" && extension != ".prefab" && extension != ".unity") continue;
                foreach (string dependency in AssetDatabase.GetDependencies(candidate, false))
                    if (dependency == assetPath) { found.Add(candidate); break; }
            }
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var component in root.GetComponentsInChildren<Component>(true))
                    {
                        if (component == null) continue;
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        while (property.Next(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == asset)
                            {
                                found.Add("열린 씬: " + (string.IsNullOrEmpty(scene.path) ? scene.name : scene.path));
                                break;
                            }
                        serialized.Dispose();
                    }
            }
            return string.Join("\n", found);
        }
    }
}

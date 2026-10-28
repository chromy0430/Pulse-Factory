using System;
using System.Collections.Generic;
using PulseFactory.Progression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PulseFactory.EditorTools
{
    [InitializeOnLoad]
    public static class ProgressionCatalogSync
    {
        public const string Path = "Assets/Resources/PFProgressionCatalog.asset";
        private static bool pending;
        static ProgressionCatalogSync()
        {
            EditorApplication.projectChanged += Schedule;
            EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.ExitingEditMode) Synchronize(); };
            Schedule();
        }
        private static void Schedule()
        {
            if (pending) return;
            pending = true;
            EditorApplication.delayCall += () => { pending = false; Synchronize(); };
        }
        public static ProgressionCatalogSO Synchronize()
        {
            var nodes = new List<ProgressionNodeSO>();
            foreach (string guid in AssetDatabase.FindAssets("t:ProgressionNodeSO", new[] { "Assets" }))
            {
                var node = AssetDatabase.LoadAssetAtPath<ProgressionNodeSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (node == null) continue;
                if (node.id != guid) { node.id = guid; EditorUtility.SetDirty(node); AssetDatabase.SaveAssetIfDirty(node); }
                nodes.Add(node);
            }
            nodes.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            var catalog = AssetDatabase.LoadAssetAtPath<ProgressionCatalogSO>(Path);
            if (catalog == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                catalog = ScriptableObject.CreateInstance<ProgressionCatalogSO>();
                AssetDatabase.CreateAsset(catalog, Path);
            }
            bool changed = catalog.nodes.Count != nodes.Count;
            for (int i = 0; !changed && i < nodes.Count; i++) changed = catalog.nodes[i] != nodes[i];
            if (changed)
            {
                catalog.nodes = nodes;
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
                ProgressionNodeSO.NotifyChanged();
            }
            return catalog;
        }
        public static ProgressionNodeSO Create(ProgressionKind kind, Vector2 position)
        {
            const string folder = "Assets/6_Scripts/Data/SO/Progression";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/6_Scripts/Data/SO", "Progression");
            var node = ScriptableObject.CreateInstance<ProgressionNodeSO>();
            node.kind = kind;
            node.title = kind == ProgressionKind.Research ? "새 연구" : "새 특성";
            node.position = position;
            AssetDatabase.CreateAsset(node, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + kind + ".asset"));
            Synchronize();
            return node;
        }
        public static bool Link(ProgressionNodeSO parent, ProgressionNodeSO child, ProgressionCatalogSO catalog, out string error)
        {
            error = null;
            if (parent == null || child == null || parent == child) { error = "다른 노드를 선택하세요."; return false; }
            if (child.prerequisites.Contains(parent)) { error = "이미 연결되어 있습니다."; return false; }
            Undo.RecordObject(child, "Connect progression nodes");
            child.prerequisites.Add(parent);
            error = catalog.Validate(child);
            if (error != null) { child.prerequisites.Remove(parent); return false; }
            Save(child);
            return true;
        }
        public static void Save(ProgressionNodeSO node)
        {
            EditorUtility.SetDirty(node);
            AssetDatabase.SaveAssetIfDirty(node);
            ProgressionNodeSO.NotifyChanged();
        }
    }
    public sealed class ProgressionBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -90;
        public void OnPreprocessBuild(BuildReport report) => ProgressionCatalogSync.Synchronize();
    }
}

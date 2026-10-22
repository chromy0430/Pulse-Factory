using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PulseFactory.EditorTools
{
    [InitializeOnLoad]
    public static class BuildMenuCatalogSync
    {
        public const string CatalogPath = "Assets/Resources/PFBuildMenuCatalog.asset";
        private static bool pending;
        static BuildMenuCatalogSync()
        {
            EditorApplication.projectChanged += Schedule;
            EditorApplication.playModeStateChanged += state =>
            { if (state == PlayModeStateChange.ExitingEditMode) Synchronize(); };
            Schedule();
        }

        private static void Schedule()
        {
            if (pending || EditorApplication.isPlayingOrWillChangePlaymode) return;
            pending = true;
            EditorApplication.delayCall += () => { pending = false; Synchronize(); };
        }

        public static BuildMenuCatalogSO Synchronize()
        {
            if (EditorApplication.isPlaying) return AssetDatabase.LoadAssetAtPath<BuildMenuCatalogSO>(CatalogPath);
            var definitions = new List<BuildingDefinitionSO>();
            foreach (string guid in AssetDatabase.FindAssets("t:BuildingDefinitionSO", new[] { "Assets" }))
            {
                var definition = AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null && definition.ShowInBuildMenu) definitions.Add(definition);
            }
            definitions.Sort((a, b) =>
            {
                int order = a.BuildMenuOrder.CompareTo(b.BuildMenuOrder);
                return order != 0 ? order : string.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b), StringComparison.Ordinal);
            });
            var catalog = AssetDatabase.LoadAssetAtPath<BuildMenuCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                catalog = ScriptableObject.CreateInstance<BuildMenuCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("buildings");
            bool equal = entries.arraySize == definitions.Count;
            for (int i = 0; equal && i < entries.arraySize; i++)
                equal = entries.GetArrayElementAtIndex(i).objectReferenceValue == definitions[i];
            if (!equal)
            {
                entries.arraySize = definitions.Count;
                for (int i = 0; i < definitions.Count; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
            return catalog;
        }
    }

    public sealed class BuildMenuCatalogBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;
        public void OnPreprocessBuild(BuildReport report) => BuildMenuCatalogSync.Synchronize();
    }
}

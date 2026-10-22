using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodexHarness
{
    public sealed class CodexHarnessSceneReport
    {
        public int MissingScripts;
        public int MissingReferences;
        public int UnassignedReferences;
        public readonly List<string> Issues = new List<string>();
        public int Total => Issues.Count;
    }

    public static class CodexHarnessSceneValidator
    {
        [MenuItem("Codex Harness/Validate Open Scenes")]
        public static void ValidateFromMenu() => ValidateOpenScenes();

        public static int ValidateOpenScenes()
        {
            var report = new CodexHarnessSceneReport();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                    InspectObject(root, scene.path, report);
            }
            foreach (string issue in report.Issues)
                Debug.LogError("[CodexHarness] FAIL: " + issue);
            if (report.Total == 0)
                Debug.Log("[CodexHarness] PASS: open scene checks passed.");
            else
                Debug.LogError("[CodexHarness] FAIL: " + report.Total + " issue(s). Missing Scripts: " +
                    report.MissingScripts + ", Missing References: " + report.MissingReferences +
                    ", Required Unassigned References: " + report.UnassignedReferences);
            return report.Total;
        }

        public static CodexHarnessSceneReport Inspect(GameObject root)
        {
            var report = new CodexHarnessSceneReport();
            InspectObject(root, root.scene.path, report);
            return report;
        }

        private static void InspectObject(GameObject target, string scenePath, CodexHarnessSceneReport report)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);
            report.MissingScripts += count;
            for (int i = 0; i < count; i++)
                report.Issues.Add("Missing Script on " + HierarchyPath(target) + " in " + scenePath);
            foreach (Component component in target.GetComponents<Component>())
            {
                if (component == null) continue;
                try
                {
                    InspectReferences(component, scenePath, report);
                }
                catch (Exception exception)
                {
                    report.Issues.Add("Reference inspection failed on " + HierarchyPath(target) +
                                     " (" + component.GetType().Name + "): " + exception.Message);
                }
            }
            for (int i = 0; i < target.transform.childCount; i++)
                InspectObject(target.transform.GetChild(i).gameObject, scenePath, report);
        }

        private static void InspectReferences(Component component, string scenePath,
            CodexHarnessSceneReport report)
        {
            using (var serialized = new SerializedObject(component))
            {
                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    // Avoid cycles inside SerializeReference graphs.
                    enterChildren = property.propertyType != SerializedPropertyType.ManagedReference;
                    if (property.propertyType != SerializedPropertyType.ObjectReference ||
                        property.propertyPath == "m_Script" || property.objectReferenceValue != null)
                        continue;
                    string kind;
                    if (property.objectReferenceInstanceIDValue != 0)
                    {
                        report.MissingReferences++;
                        kind = "Missing Reference";
                    }
                    else if (CodexHarnessReferenceRules.IsRequired(component.GetType(), property.propertyPath))
                    {
                        report.UnassignedReferences++;
                        kind = "Required Unassigned Reference";
                    }
                    else continue;
                    report.Issues.Add(kind + " on " + HierarchyPath(component.gameObject) +
                        " (" + component.GetType().Name + "." + property.propertyPath + ") in " + scenePath);
                }
            }
        }

        private static string HierarchyPath(GameObject target)
        {
            string path = target.name;
            for (Transform parent = target.transform.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }
    }
}

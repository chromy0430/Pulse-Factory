using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodexHarness
{
    public static class CodexHarnessSceneValidator
    {
        [MenuItem("Codex Harness/Validate Open Scenes")]
        public static void ValidateFromMenu()
        {
            ValidateOpenScenes();
        }

        public static int ValidateOpenScenes()
        {
            ValidationCounts counts = new ValidationCounts();

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);

                if (!scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();

                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    ValidateObject(
                        roots[rootIndex],
                        scene.path,
                        counts
                    );
                }
            }

            if (counts.Total == 0)
            {
                Debug.Log(
                    "[CodexHarness] PASS: open scene checks passed."
                );
            }
            else
            {
                Debug.LogError(
                    "[CodexHarness] FAIL: " +
                    counts.Total +
                    " validation issue(s) found.\n" +
                    "Missing Scripts: " + counts.MissingScripts + "\n" +
                    "Missing References: " + counts.MissingReferences + "\n" +
                    "Unassigned References: " + counts.UnassignedReferences
                );
            }

            return counts.Total;
        }

        private static void ValidateObject(
            GameObject target,
            string scenePath,
            ValidationCounts counts)
        {
            ValidateMissingScripts(
                target,
                scenePath,
                counts
            );

            ValidateComponentReferences(
                target,
                scenePath,
                counts
            );

            for (int i = 0; i < target.transform.childCount; i++)
            {
                ValidateObject(
                    target.transform.GetChild(i).gameObject,
                    scenePath,
                    counts
                );
            }
        }

        private static void ValidateMissingScripts(
            GameObject target,
            string scenePath,
            ValidationCounts counts)
        {
            int missingScriptCount =
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);

            if (missingScriptCount <= 0)
                return;

            counts.MissingScripts += missingScriptCount;

            Debug.LogError(
                "[CodexHarness] Missing Script x" +
                missingScriptCount +
                "\nObject: " +
                GetHierarchyPath(target) +
                "\nScene: " +
                scenePath,
                target
            );
        }

        private static void ValidateComponentReferences(
            GameObject target,
            string scenePath,
            ValidationCounts counts)
        {
            Component[] components = target.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                // Missing Script는 위에서 이미 검사했으므로 건너뜀
                if (component == null)
                    continue;

                ValidateSerializedReferences(
                    component,
                    target,
                    scenePath,
                    counts
                );
            }
        }

        private static void ValidateSerializedReferences(
            Component component,
            GameObject target,
            string scenePath,
            ValidationCounts counts)
        {
            SerializedObject serializedObject;

            try
            {
                serializedObject = new SerializedObject(component);
            }
            catch (Exception)
            {
                return;
            }

            SerializedProperty property = serializedObject.GetIterator();

            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = true;

                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                // 모든 MonoBehaviour가 가지고 있는 정상적인 Script 참조
                if (property.propertyPath == "m_Script")
                    continue;

                UnityEngine.Object reference = property.objectReferenceValue;

                if (reference != null)
                    continue;

                /*
                 * objectReferenceValue == null 이면서
                 * Instance ID가 남아있으면
                 *
                 * 기존에는 연결되어 있었지만 대상이 삭제된
                 * 실제 Missing Reference이다.
                 */
                if (property.objectReferenceInstanceIDValue != 0)
                {
                    counts.MissingReferences++;

                    Debug.LogError(
                        "[CodexHarness] Missing Reference" +
                        "\nObject: " + GetHierarchyPath(target) +
                        "\nComponent: " + component.GetType().Name +
                        "\nProperty: " + property.propertyPath +
                        "\nScene: " + scenePath,
                        target
                    );

                    continue;
                }

                /*
                 * Instance ID도 0이면 그냥 None 상태이다.
                 *
                 * 하지만 Unity 내장 Component의 경우
                 * None이 정상인 필드가 많으므로,
                 * Assets 폴더 안에 존재하는 사용자 MonoBehaviour만 검사한다.
                 */
                if (!IsProjectMonoBehaviour(component))
                    continue;

                counts.UnassignedReferences++;

                Debug.LogError(
                    "[CodexHarness] Unassigned Reference" +
                    "\nObject: " + GetHierarchyPath(target) +
                    "\nComponent: " + component.GetType().Name +
                    "\nProperty: " + property.propertyPath +
                    "\nScene: " + scenePath,
                    target
                );
            }
        }

        private static bool IsProjectMonoBehaviour(Component component)
        {
            MonoBehaviour monoBehaviour = component as MonoBehaviour;

            if (monoBehaviour == null)
                return false;

            MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);

            if (script == null)
                return false;

            string scriptPath = AssetDatabase.GetAssetPath(script);

            if (string.IsNullOrEmpty(scriptPath))
                return false;

            return scriptPath.StartsWith(
                "Assets/",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static string GetHierarchyPath(GameObject target)
        {
            string path = target.name;
            Transform current = target.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private sealed class ValidationCounts
        {
            public int MissingScripts;
            public int MissingReferences;
            public int UnassignedReferences;

            public int Total
            {
                get
                {
                    return MissingScripts +
                           MissingReferences +
                           UnassignedReferences;
                }
            }
        }
    }
}
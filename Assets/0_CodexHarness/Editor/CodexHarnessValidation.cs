using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CodexHarness
{
    public static class CodexHarnessValidation
    {
        [MenuItem("Codex Harness/Validate All")]
        public static void ValidateFromMenu() => ValidateAll();

        public static int ValidateAll()
        {
            int failures = CodexHarnessProjectValidator.ValidateAndLog();
            failures += CodexHarnessArchitectureValidator.ValidateAndLog();
            failures += CodexHarnessSceneValidator.ValidateOpenScenes();
            Debug.Log("[CodexHarness] " + (failures == 0 ? "PASS" : "FAIL") +
                      ": synchronous validation completed, " + failures + " issue(s).");
            return failures;
        }

        public static void ValidateAndExit()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogError("[CodexHarness] ValidateAndExit is batchmode-only.");
                return;
            }
            // Open enabled build scenes only in the isolated batchmode process.
            int opened = 0;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                EditorSceneManager.OpenScene(scene.path, opened++ == 0 ?
                    OpenSceneMode.Single : OpenSceneMode.Additive);
            }
            int failures = ValidateAll();
            if (opened == 0)
            {
                Debug.LogWarning("[CodexHarness] NOT VERIFIED: no enabled build scenes for scene validation.");
                EditorApplication.Exit(failures == 0 ? 2 : 1);
                return;
            }
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CodexHarness
{
    public static class CodexHarnessProjectValidator
    {
        private static readonly string[] RequiredDirectories =
        {
            "Assets/CodexHarness/Editor",
            "Assets/CodexHarness/Tests/Editor",
            "Tools/Codex",
            "docs/architecture",
            "docs/workflows"
        };

        [MenuItem("Codex Harness/Validate Project")]
        public static void ValidateFromMenu()
        {
            ValidateAndLog();
        }

        public static void ValidateAndExit()
        {
            EditorApplication.Exit(ValidateAndLog());
        }

        public static int ValidateAndLog()
        {
            List<string> failures = CollectFailures();
            if (failures.Count == 0)
            {
                Debug.Log("[CodexHarness] PASS: project structure and metadata checks passed.");
                return 0;
            }

            for (int i = 0; i < failures.Count; i++)
            {
                Debug.LogError("[CodexHarness] FAIL: " + failures[i]);
            }

            return failures.Count;
        }

        public static List<string> CollectFailures()
        {
            List<string> failures = new List<string>();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string versionPath = Path.Combine(projectRoot, "ProjectSettings/ProjectVersion.txt");
            string manifestPath = Path.Combine(projectRoot, "Packages/manifest.json");

            if (!File.Exists(versionPath)) failures.Add("ProjectSettings/ProjectVersion.txt is missing.");
            if (!File.Exists(manifestPath)) failures.Add("Packages/manifest.json is missing.");

            for (int i = 0; i < RequiredDirectories.Length; i++)
            {
                string path = Path.Combine(projectRoot, RequiredDirectories[i]);
                if (!Directory.Exists(path)) failures.Add("Required directory is missing: " + RequiredDirectories[i]);
            }

            if (File.Exists(versionPath))
            {
                string version = File.ReadAllText(versionPath);
                if (version.IndexOf("6000.3.19f1", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Debug.LogWarning("[CodexHarness] Project version differs from the inspected baseline; re-check package/API assumptions.");
                }
            }

            return failures;
        }
    }
}

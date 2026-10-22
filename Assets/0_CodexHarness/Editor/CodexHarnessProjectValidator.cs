using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CodexHarness
{
    public static class CodexHarnessProjectValidator
    {
        [MenuItem("Codex Harness/Validate Project")]
        public static void ValidateFromMenu() => ValidateAndLog();

        public static void ValidateAndExit()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogError("[CodexHarness] ValidateAndExit is batchmode-only.");
                return;
            }
            EditorApplication.Exit(ValidateAndLog() == 0 ? 0 : 1);
        }

        public static int ValidateAndLog()
        {
            List<string> failures = CollectFailures();
            foreach (string failure in failures)
                Debug.LogError("[CodexHarness] FAIL: " + failure);
            if (failures.Count == 0)
                Debug.Log("[CodexHarness] PASS: project structure and metadata checks passed.");
            return failures.Count;
        }

        public static List<string> CollectFailures()
        {
            var failures = new List<string>();
            try
            {
                var settings = CodexHarnessSettings.Load();
                foreach (string file in settings.RequiredFiles())
                    if (!File.Exists(CodexHarnessSettings.ResolvePath(file)))
                        failures.Add("Required file is missing: " + file);
                string versionPath = CodexHarnessSettings.ResolvePath("ProjectSettings/ProjectVersion.txt");
                string manifestPath = CodexHarnessSettings.ResolvePath("Packages/manifest.json");
                if (!File.Exists(versionPath)) failures.Add("ProjectVersion.txt is missing.");
                else if (!HasExpectedVersion(versionPath, settings.expectedUnityVersion))
                    failures.Add("Unity version differs from HarnessConfig.json.");
                if (!File.Exists(manifestPath)) failures.Add("Packages/manifest.json is missing.");
                settings.SourceFiles();
            }
            catch (Exception exception)
            {
                failures.Add(exception.Message);
            }
            return failures;
        }

        private static bool HasExpectedVersion(string path, string expected)
        {
            foreach (string line in File.ReadAllLines(path))
                if (line.StartsWith("m_EditorVersion:", StringComparison.Ordinal))
                    return line.Substring("m_EditorVersion:".Length).Trim() == expected;
            return false;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CodexHarness
{
    public static class CodexHarnessArchitectureValidator
    {
        [MenuItem("Codex Harness/Validate Architecture")]
        public static void ValidateFromMenu()
        {
            ValidateAndLog();
        }

        public static int ValidateAndLog()
        {
            List<string> failures = new List<string>();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string[] roots = { "Assets/Scripts", "Assets/CodexHarness" };

            for (int i = 0; i < roots.Length; i++)
            {
                string root = Path.Combine(projectRoot, roots[i]);
                if (!Directory.Exists(root)) continue;
                string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
                for (int j = 0; j < files.Length; j++)
                {
                    string[] lines = File.ReadAllLines(files[j]);
                    if (lines.Length > 300) failures.Add("Script exceeds 300 lines: " + files[j]);
                    for (int line = 0; line < lines.Length; line++)
                    {
                        string trimmedLine = lines[line].TrimStart();
                        if (trimmedLine == "using System.Linq;")
                        {
                            failures.Add("Use ZLinq or an explicit loop instead of System.Linq in first-party code: " + files[j]);
                            break;
                        }
                    }
                }
            }

            ValidateFactoryRuleDefinitions(projectRoot, failures);

            if (failures.Count == 0) Debug.Log("[CodexHarness] PASS: architecture checks passed.");
            for (int i = 0; i < failures.Count; i++) Debug.LogError("[CodexHarness] FAIL: " + failures[i]);
            return failures.Count;
        }

        private static void ValidateFactoryRuleDefinitions(string projectRoot, List<string> failures)
        {
            string definitionsRoot = Path.Combine(projectRoot, "Assets/Scenes/FactoryRule/Definitions");
            if (!Directory.Exists(definitionsRoot)) return;

            string[] files = Directory.GetFiles(definitionsRoot, "*.asset", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(files[i]);
                if (fileName == "GoodBox" || fileName == "DefectiveBox") continue;
                if (fileName.StartsWith("GoodBox ") || fileName.StartsWith("DefectiveBox "))
                {
                    failures.Add("Factory Rule definitions must be unique. Remove duplicate asset: " + files[i]);
                }
            }
        }
    }
}

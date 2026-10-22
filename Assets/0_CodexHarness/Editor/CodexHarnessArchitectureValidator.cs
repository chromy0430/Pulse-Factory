using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CodexHarness
{
    public static class CodexHarnessArchitectureValidator
    {
        private static readonly Regex LinqImport = new Regex(
            @"^\s*(?:global\s+)?using\s+(?:(?:static\s+|[A-Za-z_]\w*\s*=\s*))?System\.Linq\b");

        [MenuItem("Codex Harness/Validate Architecture")]
        public static void ValidateFromMenu() => ValidateAndLog();

        public static int ValidateAndLog()
        {
            List<string> failures = CollectFailures();
            foreach (string failure in failures)
                Debug.LogError("[CodexHarness] FAIL: " + failure);
            if (failures.Count == 0)
                Debug.Log("[CodexHarness] PASS: architecture checks passed.");
            return failures.Count;
        }

        public static List<string> CollectFailures()
        {
            var failures = new List<string>();
            try
            {
                var settings = CodexHarnessSettings.Load();
                foreach (string file in settings.SourceFiles())
                    failures.AddRange(InspectSource(file, File.ReadAllLines(file), settings.maxScriptLines));
            }
            catch (Exception exception)
            {
                failures.Add(exception.Message);
            }
            return failures;
        }

        public static List<string> InspectSource(string path, string[] lines, int maxLines)
        {
            var failures = new List<string>();
            if (lines.Length > maxLines)
                failures.Add("Script exceeds " + maxLines + " lines (" + lines.Length + "): " + path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (!LinqImport.IsMatch(lines[i])) continue;
                failures.Add("Use an explicit loop or installed ZLinq instead of System.Linq: " + path +
                             ":" + (i + 1));
                break;
            }
            return failures;
        }
    }
}

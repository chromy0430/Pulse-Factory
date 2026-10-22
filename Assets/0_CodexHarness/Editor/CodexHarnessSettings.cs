using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CodexHarness
{
    [Serializable]
    public sealed class CodexHarnessSettings
    {
        public int schemaVersion;
        public string expectedUnityVersion;
        public string harnessRoot;
        public string[] sourceRoots;
        public int maxScriptLines;

        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        public const string ConfigPath = "Tools/Codex/HarnessConfig.json";

        public static CodexHarnessSettings Load()
        {
            string path = Path.Combine(ProjectRoot, ConfigPath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Harness configuration is missing.", path);
            var settings = JsonUtility.FromJson<CodexHarnessSettings>(File.ReadAllText(path));
            if (settings == null || settings.schemaVersion != 1 || settings.maxScriptLines < 1 ||
                string.IsNullOrWhiteSpace(settings.expectedUnityVersion) ||
                settings.sourceRoots == null || settings.sourceRoots.Length == 0)
                throw new InvalidDataException("Invalid HarnessConfig.json.");
            ResolvePath(settings.harnessRoot);
            foreach (string sourceRoot in settings.sourceRoots)
                ResolvePath(sourceRoot);
            return settings;
        }

        public static string ResolvePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                throw new InvalidDataException("Expected a project-relative path: " + relativePath);
            string root = Path.GetFullPath(ProjectRoot) + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, relativePath));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Path escapes the project: " + relativePath);
            return path;
        }

        public string[] RequiredFiles()
        {
            return new[]
            {
                "AGENTS.md", "PLANS.md", ConfigPath,
                "Tools/Codex/HarnessCommon.ps1", "Tools/Codex/ValidateProject.ps1",
                "Tools/Codex/RunTests.ps1", "Tools/Codex/VerifyChanges.ps1",
                "docs/architecture/README.md", "docs/conventions/README.md",
                "docs/workflows/validation.md",
                harnessRoot + "/Editor/CodexHarness.Editor.asmdef",
                harnessRoot + "/Tests/Editor/CodexHarness.Editor.Tests.asmdef",
                ".agents/skills/unity-feature/SKILL.md", ".agents/skills/unity-debug/SKILL.md",
                ".agents/skills/unity-test/SKILL.md", ".agents/skills/unity-code-review/SKILL.md",
                ".agents/skills/unity-scene-validation/SKILL.md",
                ".agents/skills/harness-retrospective/SKILL.md"
            };
        }

        public List<string> SourceFiles()
        {
            var files = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string sourceRoot in sourceRoots)
            {
                string directory = ResolvePath(sourceRoot);
                if (!Directory.Exists(directory))
                    throw new DirectoryNotFoundException("Source root is missing: " + sourceRoot);
                string[] candidates = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
                if (candidates.Length == 0)
                    throw new InvalidDataException("Source root contains no C# scripts: " + sourceRoot);
                foreach (string path in candidates)
                    if (seen.Add(path)) files.Add(path);
            }
            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }
    }
}

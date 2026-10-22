using System.IO;
using NUnit.Framework;

namespace CodexHarness.Tests
{
    public sealed class CodexHarnessSmokeTests
    {
        [Test]
        public void ProjectStructure_IsPresent()
        {
            Assert.That(CodexHarnessProjectValidator.CollectFailures(), Is.Empty);
        }

        [Test]
        public void HarnessScripts_StayWithinConfiguredSizeBudget()
        {
            var settings = CodexHarnessSettings.Load();
            string path = CodexHarnessSettings.ResolvePath(settings.harnessRoot);
            foreach (string file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
                Assert.That(File.ReadAllLines(file).Length, Is.LessThanOrEqualTo(settings.maxScriptLines), file);
        }

        [Test]
        public void SourceRoots_IncludeActualGameplayScripts()
        {
            var settings = CodexHarnessSettings.Load();
            string expected = CodexHarnessSettings.ResolvePath("Assets/6_Scripts/Build/BuildController.cs");
            Assert.That(settings.SourceFiles(), Does.Contain(expected));
        }

        [Test]
        public void MissingSourceRoot_CannotProducePass()
        {
            var settings = new CodexHarnessSettings { sourceRoots = new[] { "Assets/__HarnessAbsentRoot" } };
            Assert.Throws<DirectoryNotFoundException>(() => settings.SourceFiles());
        }

        [Test]
        public void ConfigPaths_CannotEscapeProject()
        {
            Assert.Throws<InvalidDataException>(() => CodexHarnessSettings.ResolvePath("../OtherProject"));
        }
    }
}

using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace CodexHarness.Tests
{
    public sealed class CodexHarnessSmokeTests
    {
        [Test]
        public void ProjectStructure_IsPresent()
        {
            Assert.That(CodexHarness.CodexHarnessProjectValidator.CollectFailures(), Is.Empty);
        }

        [Test]
        public void FirstPartyScripts_StayWithinSizeBudget()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string scripts = Path.Combine(root, "Assets/CodexHarness");
            string[] files = Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                Assert.That(File.ReadAllLines(files[i]).Length, Is.LessThanOrEqualTo(300), files[i]);
            }
        }
    }
}

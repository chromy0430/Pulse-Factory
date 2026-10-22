using NUnit.Framework;

namespace CodexHarness.Tests
{
    public sealed class CodexHarnessArchitectureTests
    {
        [TestCase("using System.Linq;")]
        [TestCase("  using System.Linq; // forbidden import")]
        [TestCase("using static System.Linq.Enumerable;")]
        [TestCase("using Queries = System.Linq;")]
        [TestCase("global using System.Linq;")]
        public void LinqImport_IsRejected(string line)
        {
            Assert.That(CodexHarnessArchitectureValidator.InspectSource("fixture.cs", new[] { line }, 300),
                Has.Count.EqualTo(1));
        }

        [TestCase("using System.Collections.Generic;")]
        [TestCase("// using System.Linq;")]
        public void AllowedSource_IsAccepted(string line)
        {
            Assert.That(CodexHarnessArchitectureValidator.InspectSource("fixture.cs", new[] { line }, 300),
                Is.Empty);
        }

        [TestCase(300, 0)]
        [TestCase(301, 1)]
        public void SizeBudget_UsesPhysicalLineCount(int lines, int expectedFailures)
        {
            var source = new string[lines];
            for (int i = 0; i < lines; i++) source[i] = "";
            Assert.That(CodexHarnessArchitectureValidator.InspectSource("fixture.cs", source, 300),
                Has.Count.EqualTo(expectedFailures));
        }
    }
}

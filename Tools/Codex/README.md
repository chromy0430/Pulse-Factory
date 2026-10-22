# Codex Harness tools

Configuration: HarnessConfig.json. Project root is resolved relative to this directory,
so spaces in "Pulse Factory" and the current working directory do not change the target.

From the project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\Codex\ValidateProject.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\Codex\VerifyChanges.ps1"
```

Fast validation is static-only; the current gameplay has three pre-existing size violations,
so exit 1 is expected until those scripts are intentionally refactored.

With Unity open use Codex Harness/Validate All and built-in Unity MCP for compile/tests.
For Editor tests select assembly CodexHarness.Editor.Tests.

With this project closed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\Codex\ValidateProject.ps1" -Full -UnityPath "<Unity.exe>"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\Codex\RunTests.ps1" -UnityPath "<Unity.exe>" -TestMode EditMode
```

RunTests defaults to CodexHarness.Tests; change -Filter for a relevant different suite.
Batch commands do not launch against a locked project.
Exit 0 = selected gates passed, 1 = failure, 2 = unavailable/unexecuted gate.
See docs/workflows/validation.md and harness-verification.md.

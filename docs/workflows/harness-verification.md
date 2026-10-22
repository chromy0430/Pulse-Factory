# Migration verification — 2026-10-14

Target observed through built-in Unity MCP: E:/GitHub/Pulse Factory,
Unity 6000.3.24f1; loaded scene Assets/1_Scenes/Main.unity.
Source 3DHarness remained read-only; imported harness folder/GUIDs were retained.

## Before

Imported harness smoke tests: 0/2 passed.
One looked for absent Assets/CodexHarness; the other expected missing root Tools/docs.
Architecture roots did not cover the actual Assets/6_Scripts folder.

## After

- Recompile: completed, compilationFailed=false, errors=[].
- Harness EditMode assembly: 19 total, 19 passed, 0 failed, 0 skipped.
- Regression coverage: Missing Script, destroyed Component reference, hidden/inactive child,
  Odin required None, optional None, actual script roots, missing root rejection,
  size boundary, import forms and project-path containment.
- Validate Project: PASS.
- Validate Open Scenes on Main.unity: PASS, zero reported issues.
- Validate Architecture / Validate All: FAIL on these unchanged authored scripts:
  - Assets/6_Scripts/Event/PowerCable.cs — 311 physical lines.
  - Assets/6_Scripts/Event/PowerNode.cs — 527 physical lines.
  - Assets/6_Scripts/UI/PulseChargeUI.cs — 321 physical lines.
- PowerShell fast validation found the same three violations and exited 1.
- Four PowerShell scripts parsed without syntax errors.
- NUnit summary helper distinguished passed, failed and zero-test reports.
- RunTests.ps1 and -Full correctly refused the open project. No second Editor was launched.
- Git status/whitespace checks passed. Gameplay, scenes, packages and settings have no changes.
- Main.unity remained not dirty; no __Harness test objects remained after tests.

## Limits

Closed-project batch execution and Player/PlayMode/build/performance gates were not executed.
They are NOT VERIFIED; the successful Editor tests do not establish those gates.
Scene checks cover loaded GameObjects/components, not unopened scenes, prefab assets,
referenced ScriptableObject internals or SerializeReference graph children.
Import enforcement is a source-text rule, not full C# semantic dependency analysis.
FAIL logs produced by the architecture menu are validation findings, not compiler failures.

ContextForge registry identity was resolved for this project, but the service tool was
unavailable in this chat; no memory bootstrap or promotion was performed.

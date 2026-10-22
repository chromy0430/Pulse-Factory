# Harness migration: 3DHarness -> Pulse Factory

Source: E:/GitHub/3DHarness. Source project remains unchanged.
Imported Editor/Test assets were found in Assets/0_CodexHarness; existing GUIDs/asmdefs are retained.
Missing root-level tools, documentation and task workflows are installed in this project.

Observed before changes:
- Live Editor confirmed Pulse Factory, Unity 6000.3.24f1, Pipeline 0.8.0-exp.1.
- Main.unity was loaded; project Git status was clean.
- Both imported smoke tests failed because they referenced Assets/CodexHarness.
- ProjectValidator expected old harness directories and missing Tools/docs.
- ArchitectureValidator scanned Assets/Scripts and Assets/CodexHarness rather than Assets/6_Scripts.
- SceneValidator treated every empty reference under Assets as required, including vendor/optional fields.

Changes:
- Shared configuration for relocated harness root, source roots and Unity baseline.
- Preserve Project/Architecture/Scene checks and add a synchronous Validate All menu.
- Missing Script and broken-reference checks remain separate; required None uses Odin contracts.
- Static PowerShell now runs while Editor is open; offline batch/test runners check lock/results.
- Six existing task workflows are adapted to the built-in Unity MCP route.
- Regression tests exercise coverage and failures without modifying production scenes.

Scope: harness migration and verification, not gameplay refactoring or artificial demo errors.
Architecture can correctly FAIL on pre-existing oversized gameplay scripts.
Final evidence and exact remaining gates are recorded in docs/workflows/harness-verification.md.

# Pulse Factory project harness

Unity project root: this directory (Assets, Packages, ProjectSettings).
Authored gameplay: Assets/6_Scripts. Harness: Assets/0_CodexHarness.
Shared validator configuration: Tools/Codex/HarnessConfig.json.
Use current files for Unity/package facts; the configuration records the inspected baseline.

## Working rules

- Follow the requested scope; preserve unrelated and in-progress changes.
- Inspect existing code, dependencies, scenes and serialization before editing.
- Use the Unity CLI built-in MCP server (unity mcp; mcp__unity__) for live observations.
  Confirm editor_status points to this project before mutations. Do not use the old embedded
  mcp-unity bridge or copy its connection settings from 3DHarness.
- Keep authored C# within 300 lines; report pre-existing violations separately instead of
  refactoring unrelated gameplay merely to obtain PASS.
- Prefer explicit references, owned lifetimes and existing Input System/event patterns.
- Use DOTween, Odin, ZLinq, R3 or UniTask only if the current project actually provides them.
  Do not install dependencies just to satisfy a generic convention.
- Preserve asset GUIDs, scene wiring and prefab connections. Use Unity tools for serialized assets.
- Required object references follow existing Odin [Required] field contracts.
  Optional None fields are valid; a dangling reference and a Missing Script are separate failures.

## Workflow

Inspect -> Plan if needed -> Implement -> Compile -> Relevant tests ->
Project/Architecture/Scene checks -> Console -> Diff review -> Revalidate if changed.

Fast static checks: Tools/Codex/ValidateProject.ps1.
Live synchronous checks: Codex Harness/Validate All in the connected Editor.
Tests in an open Editor: MCP run_tests/Test Runner, filtered to affected assemblies.
Batchmode -Full and RunTests.ps1 are for a CLOSED project only; never start a second Editor
against the open checkout. VerifyChanges.ps1 reports Git status and whitespace checks.

## Definition of done

Requested changes are implemented and appropriate gates have actual evidence.
Report each gate as PASS, FAIL or NOT VERIFIED, with failed paths and relevant result counts.
A static PASS does not establish compilation, Play Mode, visuals, performance or build success.
Use PLANS.md and docs/workflows/validation.md for larger work.
Project-local repeatable workflows are under .agents/skills. Do not create agents automatically.

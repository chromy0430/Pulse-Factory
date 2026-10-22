# Validation workflow

## Fast, with Unity open or closed

From the project root:
`powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\Codex\ValidateProject.ps1"`

Checks metadata, required harness files, actual source roots, 300-line budget and System.Linq
import text rules. It does not launch Unity or claim compilation/Scene/test success.
Configuration comes from Tools/Codex/HarnessConfig.json. Missing/empty roots FAIL.

## Connected Editor (normal development)

Confirm editor_status identifies Pulse Factory and compilation has finished.
Run Codex Harness/Validate Project, Validate Architecture, Validate Open Scenes, or Validate All.
Validate All combines these three synchronous checks; it does not itself compile or run tests.
Use Unity MCP recompile/console_status and run_tests for those gates.
For this harness use EditMode filter_type=assembly, filter=CodexHarness.Editor.Tests.
Inspect test_status summary and new Console messages; record concrete failures.

Scene validation covers loaded scenes including inactive child objects.
It does not scan unopened scenes/prefab assets, rendered UI or performance.
Broken object references use serialized reference state; required None fields honor Odin [Required].
SerializeReference graph children are not traversed, to avoid cycles; report this coverage boundary.
Inspection exceptions become failures instead of being silently ignored.

## Closed project batchmode

`powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\Codex\ValidateProject.ps1" -Full -UnityPath "<matching Unity.exe>"`

Full adds the Editor validators on enabled build scenes and EditMode/PlayMode test commands.
No enabled scenes or no executed tests is NOT VERIFIED, never PASS.
RunTests.ps1 accepts -TestMode and -Filter (default CodexHarness.Tests).
These commands refuse a locked project; use the connected Editor when it is open.
Test reports and logs use unique filenames in Temp; both process exit and fresh NUnit results matter.
Exit codes: 0 passed selected gates, 1 failure, 2 unavailable/unexecuted gate.

## Change review

`powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\Codex\VerifyChanges.ps1"`

Displays Git status and checks staged/unstaged whitespace. Review actual diffs separately.
PASS, FAIL, NOT VERIFIED apply only to the named gate.

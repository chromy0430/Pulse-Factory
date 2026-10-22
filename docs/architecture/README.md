# Pulse Factory architecture

Existing gameplay lives under Assets/6_Scripts:
Build (grid, placement, menu), Event (power nodes/cables/pulses), Economy (credits),
UI (credit/charge display), Data/SO_Scripts (authored definitions/events/effects).
Preserve existing responsibilities and serialized field names during harness migration.

Harness Editor code is isolated by CodexHarness.Editor.asmdef.
Editor tests are isolated by CodexHarness.Editor.Tests.asmdef.
Temporary reference probes use CodexHarness.TestFixtures with UNITY_EDITOR; they are excluded
from Player builds and test objects are created in a separate temporary scene.
No new gameplay assembly boundaries, DI frameworks or packages are introduced.
Tools/Codex/HarnessConfig.json defines the real script roots and Unity baseline.

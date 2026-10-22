# Existing systems audit

- KEEP Unity 6000.3.24f1 and current URP/Input System/Test Framework packages.
- KEEP current Pipeline 0.8.0-exp.1, built-in Unity CLI MCP.
- KEEP existing Odin required field contracts and DOTween usage.
- KEEP Assets/6_Scripts, imported Assets/0_CodexHarness GUIDs/asmdefs and existing scenes.
- ADAPT 3DHarness tools/rules to these roots and the actual Unity version.
- DO NOT COPY legacy embedded MCP package, FactoryRule definitions checks or source project settings.
- DO NOT impose authored code rules on third-party folders.

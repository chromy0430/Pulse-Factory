# Conventions

- One responsibility per script; authored C# budget is 300 physical lines, including blank lines.
- MonoBehaviours own Unity lifetimes; extract reusable rules only when a real boundary needs it.
- ScriptableObjects store authored settings/definitions; mutable session state stays elsewhere.
- Pair Input System/event subscriptions with cleanup.
- Use installed animation/async/reactive libraries, not guessed dependencies.
- Architecture import checks reject System.Linq imports in configured authored roots.
  Prefer an explicit loop or installed ZLinq; no dependency is installed automatically.
- Scene reference checks distinguish Missing Script, broken references and required None fields.
  Existing Odin [Required] supplies required contracts; optional None fields remain valid.
- Vendor code under Assets/99_ThirdParty, DTT, Plugins and TextMesh Pro is outside authored size/import scans.

# Doc ↔ code map

Feeds the `.githooks/pre-commit` nag. **Not a gate** — a design doc is allowed to lag unbuilt code (see
`CLAUDE.md`'s design-gate note). When a staged file matches a **Code** glob below, the hook checks
whether at least one paired **Docs** file is also staged; if not, it prints a reminder and lets the
commit proceed. Skip it for a given commit with `git commit --no-verify` when the doc genuinely needs no
change.

Add a row when a design doc makes claims about a specific file or directory. Keep rows narrow — the goal
is a useful nag, not full coverage; a row that fires on every commit trains people to ignore it.

| Code (glob) | Docs |
|---|---|
| `Assets/Code/Runtime/Modules/Inventory/ChainResolver.cs` | `Docs/GlyphsHeroDesign/Design Docs/Inventory/Item Chaining.md`, `Docs/agents/combat-grounding.md` |
| `Assets/Code/Runtime/Modules/Inventory/ChainConnector*.cs` | `Docs/GlyphsHeroDesign/Design Docs/Inventory/Item Chaining.md` |
| `Assets/Code/Runtime/Core/Combat/*.cs` | `Docs/GlyphsHeroDesign/Design Docs/Combat/Combat.md`, `Docs/GlyphsHeroDesign/Design Docs/Combat/Attack Targeting.md`, `Docs/agents/combat-grounding.md` |
| `Assets/Code/Runtime/Core/Combat/PawnCombatController.cs` | `Docs/GlyphsHeroDesign/Design Docs/Inventory/Items/Payload.md`, `Docs/agents/combat-grounding.md` |
| `Assets/Code/Runtime/Core/Combat/CombatCoordinator.cs` | `Docs/GlyphsHeroDesign/Design Docs/Architecture Review.md`, `Docs/GlyphsHeroDesign/Design Docs/Combat/Pawn.md` |
| `Assets/Code/Data/Items/Weapon/*.cs` | `Docs/GlyphsHeroDesign/Design Docs/Inventory/Items/Weapon.md`, `Docs/GlyphsHeroDesign/Design Docs/Inventory/Items/Payload.md` |
| `Assets/Code/Data/Items/Reactor/*.cs` | `Docs/GlyphsHeroDesign/Design Docs/Inventory/Items/Reactor.md` |
| `Assets/Code/Data/Items/Shifter/*.cs` | `Docs/GlyphsHeroDesign/Design Docs/Inventory/Items/Shifter.md` |
| `Assets/Code/Data/Items/Amplifier/*.cs` | `Docs/GlyphsHeroDesign/Design Docs/Inventory/Items/Amplifier.md` |
| `Assets/Code/Data/Items/Converter/*.cs` | `Docs/GlyphsHeroDesign/Design Docs/Inventory/Items/Converter.md` |
| `Assets/Code/Data/Enums/ConverterAxis.cs` | `Docs/GlyphsHeroDesign/Design Docs/Inventory/Items/Converter.md` |
| `Assets/Code/Runtime/Pawns/*.cs` | `Docs/GlyphsHeroDesign/Design Docs/Combat/Pawn.md` |
| `Assets/Code/Runtime/Modules/Statistics/Resource.cs` | `Docs/GlyphsHeroDesign/Design Docs/KNOWN_ISSUES.md` |

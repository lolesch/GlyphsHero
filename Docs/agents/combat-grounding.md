# Combat attack-model — grounding read list

The **minimum** set to read before continuing the combat attack-model work, *on top of the ADRs*. The
ADRs give the decisions and the "why"; this list points at the live vocabulary, the design reference,
and the exact code seams so you're grounded in both design and code — not one or the other.

Read top-to-bottom. The **Core** block is the true minimum; the **As needed** block depends on what
you're touching.

## Anchor (start here)

- **`Docs/adr/0004-attack-model-item-roles-and-recursive-delivery.md`** — the consolidated model:
  item roles (Converter = type-reclassifier, Shifter = economy-trade only), **Delivery = Pattern ×
  Layers × Affinity × Anchor**, Reach = one uniform pawn stat, Propagation = recursive child-delivery.
  Its "Still deferred" list is the work queue.

## Core (the minimum)

Design / vocabulary:
- **`CONTEXT.md`** (repo root) — the glossary. The axis terms (Delivery Pattern, Affinity, Anchor,
  Reach, Trigger, Covered Hexes) and the words to *avoid*. Use these names in code and tests.
- **`Docs/GlyphsHeroDesign/Design Docs/Combat/Attack Targeting.md`** — the three-axis reference with the
  pattern + propagation tables. The design-facing companion to ADR-0004.

Code seams:
- **`Assets/Code/Data/Enums/DeliveryPattern.cs`** + **`Affinity.cs`** — the two axis enums (small; read
  both). `DeliveryPattern` is geometry only; `Affinity` is whose-side. Note bit `1 << 3` is reserved
  (was `Self`).
- **`Assets/Code/Runtime/Core/Combat/DeliveryResolver.cs`** — pure geometry, `CoveredHexes(...)`. Where a
  new Pattern (or geometry for a Layer) goes.
- **`Assets/Code/Runtime/Core/Combat/DeliveryAffinity.cs`** — pure affinity + anchor rules. Where the
  **independent Anchor axis** (a deferred item) lands; today Self is coupled to self-anchor here.
- **`Assets/Code/Runtime/Core/Combat/PawnCombatController.cs`** — the integration point:
  `Fire` / `FirePayloads` / `ResolveTargets` wire pattern + affinity + anchor into damage, and payloads
  recurse here. Where Layers (Pierce/LoS) and recursive payload anchoring get wired.

## As needed

- **`CombatCoordinator.cs`** — if touching reach / movement / the tick: `ResolveReach`, the `CombatClock`,
  movement orchestration.
- **`WeaponStats.cs`** + **`WeaponStatResolver.cs`** — if touching the **Converter** (the resolver is
  where a Converter would reclassify Delivery/Affinity).
- **ADR-0001** (reach/movement/tick), **ADR-0002** (hex-occupancy + telegraph), **ADR-0003** (delivery
  patterns) — ADR-0004's companions; read the one nearest your change. (0001 §2b withdrawn, §3 amended,
  0003's `Self` relocated — all by 0004.)
- Item design docs — **`Converter.md`**, **`Payload.md`**, **`Weapon.md`** (under
  `Docs/GlyphsHeroDesign/Design Docs/Inventory/Items/`) — when changing that item's role.

## Tests (behavioral locks + red-green templates)

- **`Assets/Code/Tests/EditMode/Combat/DeliveryResolverTests.cs`** + **`DeliveryAffinityTests.cs`** — the
  pure-seam locks; copy their shape for new pure logic (red-green / mutation-proven).
- **`ChainResolverTests.cs`** — must stay green when touching chain resolution (per `CLAUDE.md`).
- Note: `CombatCoordinator` / `PawnCombatController` have **no** unit harness (no FakePawn/registry
  fakes), so verify integration via the full EditMode suite (Unity MCP — see the architecture-review
  memory for the test-run recipe).

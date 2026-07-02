---
tags:
  - ADR
  - Combat
  - Inventory
  - Resources
  - Items
status: Accepted
date: 2026-07-02
---

# ADR-0009 — Cost modification is a generic, opt-in secondary property of any chain item

**Status:** Accepted (2026-07-02)
**Lifecycle:** Design-only — not implemented
**Companion:** ADR-0004 (attack model — item roles), ADR-0005 (resource economy — Cost / Gain-on-hit /
Magnitude), ADR-0006 (payload propagation cost economy).
**Amends:** ADR-0004 §1 (the Amplifier row: "no conditions; costs grid space" is refined to allow an
optional Cost modifier alongside its Magnitude job); ADR-0005 §1 (Cost's "who owns it: the Weapon /
Trigger" column is widened from an exclusive owner to a seedable-and-modifiable surface).
**Context:** the tooltip redesign surfaced that Reactor (`inputMod`) and Payload (its own cost-to-pool,
ADR-0006) already modify the shared Cost pool ad hoc, each wired one-off in its own resolver path.
Design intent (2026-07-02 tooltip review) wants Amplifiers to also be able to carry a Cost modifier —
some free, some raising the pool cost — as the glass-cannon/thrifty-build design space. Doing that by
giving Amplifier a second axis would violate ADR-0004's one-item-one-axis discipline; this ADR instead
generalizes the *mechanism* Reactor/Payload already use into one seam every item type can opt into.

---

## Context

Cost-modification already exists in the code, just not as one seam:

- **Reactor** carries `inputMod` (`WeaponInputModifier`) — e.g. `×120% mana` to its firing threshold.
  This is sanctioned by ADR-0005 §1: Cost is owned by "the Weapon / **Trigger**," and Reactor *is* the
  Trigger (ADR-0004 §1), so this was never a violation.
- **Payload** carries its own cost-to-pool economy (ADR-0006).
- **Amplifier** and **Converter** currently have no Cost surface at all — Amplifier is explicitly
  "no conditions; costs grid space" (ADR-0004 §1), meaning its only "cost" is the inventory slot it
  occupies, never the resource pool.

The ask is to let an Amplifier (and, generically, any item) also carry a Cost modifier — independent of
whatever its primary axis already does — so some amplifiers are free riders and others tax the pool,
the same lever Reactor/Payload already pull.

## Decisions

### 1. Cost modification reuses `WeaponInputModifier` — the exact type Reactor and Shifter already carry — Accepted

Amplifier and Converter gain an optional `inputMod` field of the existing `WeaponInputModifier` type
(`Assets\Code\Runtime\Modules\Statistics\WeaponStatModifier.cs`) — the same type `ReactorItem.inputMod`
and `ShifterItem.inputMod` already use. `WeaponStatResolver`'s existing `switch (item)` contributor loop
(`WeaponStatResolver.cs:75-93`) grows two more cases that call the same shared `ApplyInput` closure
(lines 45-61) Reactor/Shifter already call. Default is **no-op** (no `inputMod` authored) — every
existing Amplifier/Converter asset is unaffected, no data migration.

*Why:* this isn't a new mechanism — it's removing an arbitrary restriction. `WeaponInputModifier` was
never actually special to Reactor/Shifter in the code; it just happened to only be wired on those two
item types. It doesn't add a new item *role* (Amplifier is still fundamentally the Magnitude item) — it
extends a property every item type can already technically carry (the field exists; only the wiring was
role-gated) the same way "costs grid space" already applies regardless of role.

### 2. Cost modifiers compose via the existing `MutableFloat` bucket-by-type aggregation — no apply-order dependency, no "trigger applies last" — Accepted

`MutableFloat.ApplyModifiers` (`MutableFloat.cs:86-108`) already aggregates every stat's modifiers by
**type bucket**, not insertion order: all `FlatAdd` sum first, then the summed `PercentAdd` bucket
applies once, then `PercentMult` modifiers multiply in (commutative — order among them cannot change the
result). This is the *existing* rule for every stat (Damage, AttackSpeed, Cost) today, not something
this ADR introduces. So an Amplifier's new `inputMod` and a Reactor's or Shifter's existing `inputMod`
land in the **same bucketed accumulation** on the same `MutableFloat` (e.g. `resourceCost`) — there is no
ordering to decide and nothing "applies last." The chain's `OrderedItems` walk still matters for the
**tooltip's positional-delta narrative** (each piece's *marginal* contribution, for display — ADR-0004's
`PositionalDelta`), but that's a presentation concern layered on top of an already order-independent
final number, not a resolver requirement this ADR needs to invent.

*Why:* the code was already order-independent for every other stat; assuming Cost needed special
sequencing (an earlier draft of this ADR did) was an unforced error, corrected once the actual
aggregation code was checked (`MutableFloat.cs:92-107`) rather than assumed from the tooltip's display
semantics.

### 3. Cost's ownership widens from "Weapon / Trigger, exclusively" to "seeded by Weapon, contributed to by any item carrying an `inputMod`" — Accepted

ADR-0005 §1's Cost row ("who owns it: the Weapon / Trigger") is refined: the Weapon still **seeds** the
base Cost magnitude and pool (`CostResource`); any chain item — Trigger or not — may contribute an
`inputMod` term, accumulated per Decision 2. This describes what the code already permits once Decision
1's wiring is added — Reactor and Shifter were never structurally privileged, they were just the only
two types wired into the switch.

*Why:* this is the minimal widening that explains the Reactor/Shifter precedent without re-opening
ADR-0005's Gain-on-hit or Magnitude rows, which are untouched.

## Worked example

Chain: **Reactor "Quick Trigger"** (root; trigger = fires when hit; `inputMod` = `ManaCost` `PercentAdd`
`+20`) → **Amplifier "Heavy Amp"** (magnitude +4 dmg; new `inputMod` = `ManaCost` `FlatAdd` `+2`) →
**Amplifier "Free Amp"** (magnitude +1 dmg; no `inputMod` authored, i.e. default no-op) → **Weapon
"Crossblades"** (base Damage 5, base Cost 3 Mana).

Resolution — `WeaponStatResolver`'s contributor loop (`Contributors()` = root + `chain.Modifiers`, order
irrelevant per Decision 2) feeds every item's `inputMod` into the same `resourceCost` `MutableFloat` via
`ApplyInput`. `MutableFloat.ApplyModifiers` then buckets by `ModifierType`:

1. Base value = **3 Mana** (`baseValue`, seeded by the Weapon — Decision 3).
2. `FlatAdd` bucket: only Heavy Amp contributed one (+2); Free Amp contributed none. Sum = +2 →
   running total = 3 + 2 = **5 Mana**.
3. `PercentAdd` bucket: only Reactor contributed one (+20). Sum = +20% → **5 × 1.2 = 6 Mana**.
4. `PercentMult` bucket: empty, no-op.
5. Final Cost = **6 Mana** — the same result regardless of whether the loop visits Reactor, Heavy Amp,
   or Free Amp first, because `ApplyModifiers` buckets by type before combining (Decision 2).
6. Damage resolves independently through the existing output-side walk, unaffected by this ADR: base 5 +
   Heavy Amp's +4 + Free Amp's +1 = **10 dmg**.

Final: Crossblades fires for **10 dmg at 6 Mana**, triggered on being hit. The tooltip's existing
"additive — only shows when non-default" rule (2026-06-30 tooltip spec §3) means Free Amp's row shows
no Cost line at all, while Heavy Amp's row shows `+2 Mana` alongside its `+4 dmg` — no new rendering
rule needed, this slots into the piece-list format already shipped. The tooltip's *positional-delta*
narrative (each piece's marginal effect at its position, per the existing `OrderedItems` walk) still
reads left-to-right as authored — Decision 2 only establishes that the underlying **final number**
doesn't depend on that walk's order, not that the walk itself goes away.

## Considered and rejected

- **A dedicated "Cost Amplifier" item type.** Rejected — it would add a role for what's really a shared
  optional property every role can already have zero-or-more of (grid-space cost already works this
  way).
- **Keep Cost modification exclusive to Reactor/Payload** (status quo). Rejected — it's what motivated
  this ADR; the design wants free-vs-costly Amplifiers as a build lever.
- **A second resolver walk dedicated to Cost.** Rejected in favour of Decision 2 — folding into the
  existing Magnitude walk avoids two divergent apply-orders over the same list.

## Deferred (designed, not built)

- **Dual-pool cost modifiers** (an item taxing both Mana and Health at once) — still deferred per
  ADR-0005 Decision 4; this ADR doesn't reopen it.
- **Converter reclassifying which pool an `inputMod` taxes** (e.g. an Amplifier's cost-mod normally taxes
  the Weapon's `CostResource`, but a Resource-axis Converter downstream changes that pool) — falls out
  of ADR-0005 §2 unchanged, not re-litigated here.

## Open questions — resolved 2026-07-02

**Was:** does a Shifter's existing `inputMod`/`outputMod` trade compose with another item's new
`inputMod` in the same chain, or does it need to override/sequence against it?

**Resolved — it already composes, because nothing structurally special exists to conflict with.**
Checked directly against the code (`WeaponStatResolver.cs:75-93`, `ShifterItem.cs:8-22`,
`MutableFloat.cs:86-108`) rather than assumed:

- `ReactorItem.inputMod` and `ShifterItem.inputMod` are the **same `WeaponInputModifier` type**, applied
  through the **same shared `ApplyInput` closure** — Shifter was never privileged or special-cased
  relative to Reactor, and per Decision 1 an Amplifier's new `inputMod` goes through that identical path.
- Shifter's `inputMod`/`outputMod` pair has **no atomic-pairing or conservation logic** in code — they
  are two independent fields (`ShifterConfig.inputStatMod` / `outputStatMod`), each accumulated into its
  target `MutableFloat` exactly like any other contributor's modifier. "Trade" is ADR-0004's *design*
  framing for the pair, not an enforced transactional constraint (confirmed: no test asserts
  input-delta == output-delta; `WeaponStatResolverTests.Shifter_AppliesInputModToInputStat_AndOutputModToOutputStat`
  only asserts each half lands on its own target stat).
- Per Decision 2, the aggregation is bucketed by `ModifierType`, not insertion order — so a Shifter's
  `inputMod` and an Amplifier's `inputMod` simply both land in the same bucket on the same stat and sum
  (or multiply, per type) together. There is nothing to sequence and nothing to override.

This ADR is promoted to **Accepted**.

## Consequences

- **Positive:** unlocks free-vs-costly Amplifier design space without a new item role, by removing an
  arbitrary role-gate on a field type (`WeaponInputModifier`) that already existed; Cost math continues
  to use the resolver's existing bucketed `MutableFloat` aggregation with no new ordering rule; the
  tooltip's already-shipped "additive, non-default-only" rendering rule covers it with no new UI rule.
- **Negative / debts:** `AmplifierConfig`/`ConverterConfig` (and their runtime item types) gain a new
  optional `inputMod` field and a matching case in `WeaponStatResolver`'s switch — small, mechanical,
  no data migration (default no-op). Tooltip rendering for Amplifier/Converter cost-mods (blocked on
  this ADR per [needs-design #16](https://github.com/lolesch/GlyphsHero/issues/16)) can now be filed as
  a `ready-for-agent` tooltip slice.

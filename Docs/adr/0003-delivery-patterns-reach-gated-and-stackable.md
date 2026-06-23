---
tags:
  - ADR
  - Combat
  - HexGrid
  - Delivery
status: Accepted
date: 2026-06-23
---

# ADR-0003 — Delivery patterns are reach-gated, stackable, and resolve on covered hexes

**Status:** Accepted (2026-06-23)
**Companion:** ADR-0002 (hex-occupancy damage + telegraph contract), ADR-0001 (combat tick, Reach as a
pawn stat). Implements [[architecture-review-2026-06]] Candidate #5 Decision 2b (the delivery-pattern
split) and resolves the three owed-cleanup items in ADR-0002's Consequences.
**Context:** ADR-0002 ratified hex-occupancy damage but left the Delivery Pattern axis half-built — the
root weapon `Fire` was hard-locked `Single`, `PayloadTargeting` was a mis-named enum, and `Cone` had no
hex math. Designing the actual patterns surfaced that a sized cone/disk explodes combinatorially (a
radius-3 disk is 37 hexes) and fights the Reach stat instead of riding it.

## Decision

1. **`DeliveryPattern` is a stackable `[Flags]` mask.** A weapon (and a payload's child delivery)
   carries a mask; the covered hexes are the **union** of each set flag's contribution. The geometry
   is a pure function, `DeliveryResolver.CoveredHexes(origin, anchor, mask, shapeSize)` — no pawns,
   registry, grid, or engine — so it is unit-tested in isolation.

2. **No separate shape-size knob; footprints scale by Reach.** Reach (ADR-0001, a pawn stat) is purely
   the **acquisition gate**. Size-free patterns derive their footprint from the origin→anchor geometry
   and grow only because a higher Reach lets a pawn engage farther. The only size-bearing pattern is
   `Aoe`, which keeps an explicit radius (`PayloadBehavior.ShapeSize`) and is **payload-only** — no
   weapon ever paints a disk.

3. **v1 pattern set:** `Single` (the anchor's hex), `Line` (a beam from origin to anchor, origin
   excluded), `Cleave` (below), `Self` (the origin's hex) on both weapons and payloads; `Aoe` (a disk
   around the anchor) on payloads only. **`Cleave`** (aka *Swipe*) = the anchor **plus its two
   same-ring neighbours** (the two of its six neighbours at the same distance from the firing pawn):
   always exactly three hexes, no facing/angle math, diagonal anchors free, and angularly narrowing
   with distance. It **replaces the rejected `Cone`**.

4. **Uniform hex-occupancy; the `Single` direct-hit special case is removed.** `Single` covers only the
   anchor's hex, so the locked target is hit because it stands there — identical behaviour, no special
   path. `Homing` (re-resolve the anchor to the moved target at impact) remains the lone exception and
   is deferred.

## Why

A cone or disk whose size scales with Reach gets out of control fast and makes Reach a footprint dial
rather than the valuable acquisition stat ADR-0001 intended. Small, reach-scaled footprints keep Reach
valuable and the board legible. `Cleave`'s same-ring construction sidesteps the cone's real costs — the
60°-vs-120° choice, diagonal facing directions, and naming the per-distance variants — by never needing
a direction at all. Stackability makes the axis compositional (a weapon can be `Line | Cleave`) for
near-zero cost once covered hexes are a union. Dropping the `Single` special case unifies all damage on
one rule, which is exactly what ADR-0002 asked for.

## Considered and rejected

- **The `Cone`** (a 120° fan or a narrow widening wedge). Rejected: footprint scales as ~size² /
  size·(size+2) — far too many hexes at Reach 2–3 — and it needs facing/angle math plus special handling
  for diagonal facings and an awkward taxonomy of per-distance variants.
- **A separate shape-size parameter** (split out from "range"). Rejected: redundant with Reach for the
  size-free patterns and it pushes footprints large; kept only where genuinely needed (the `Aoe` radius).

## Consequences

- The root weapon now carries a `DeliveryPattern` (default `Single` = unchanged behaviour), so a
  "Beam/Cleave weapon" is expressible. New pure seam `DeliveryResolver` + `DeliveryResolverTests`
  (mutation-proven); `TargetSelector.PawnsOnHexes` is the shared occupancy step.
- **`Self` self-damage was a regression — FIXED (2026-06-23).** The self-hurt is a *deliberate* mechanic:
  a player builds around taking damage to drive `OnSelfHit`/threshold effects. Under uniform
  hostile-occupancy, `Self` covers the caster's own hex, but no enemy ever stands there, so it hit no one.
  Fixed by giving the delivery axis a **self/hostile affinity split**: the new pure
  `DeliveryResolver.SelfHexes(origin, anchor, mask)` returns the self-affinity subset of the footprint
  (`mask & SelfAffinity`, today just the origin hex). `PawnCombatController.ResolveTargets` resolves
  occupancy in two passes — the full footprint filtered to the *enemy* team (hostiles), plus the
  self-affinity hexes filtered to the caster's *own* team — so a `Self` weapon/payload hits the firing
  pawn while `Line`/`Cleave`/`Aoe` never strike allies. This is the friendly/self side of the same axis
  the aura/buff work will extend. Red-green covered by `DeliveryResolverTests.SelfHexes_*`.
- **Still deferred:** Converter reclassification of the mask; `Homing`; recursive payload child-delivery
  anchoring at the parent's impact hex (payloads still anchor on the locked target); and the
  melee/ranged Reach classification in `CombatCoordinator.ResolveMinReach`, which still uses the
  `ShapeSize ≤ 1` placeholder (Decision 2c follow-up).

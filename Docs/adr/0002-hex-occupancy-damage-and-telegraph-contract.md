---
tags:
  - ADR
  - Combat
  - HexGrid
  - Targeting
  - Delivery
status: Accepted
date: 2026-06-22
---

# ADR-0002 — The hex grid is the damage contract; deliveries resolve by occupancy and are telegraphed

**Status:** Accepted (2026-06-22)
**Lifecycle:** Implemented
**Companion:** [[CONTEXT]] (Attack resolution vocabulary), ADR-0001 (combat tick, Reach as a pawn stat).
**Context:** decided while designing the movement lerp polish, when "what is a Beam/Cone if attacks
are target-locked?" exposed that the runtime had two contradictory damage models half-built.

## Decision

An attack resolves as a sentence across three axes (Target Selection → Delivery Pattern →
Propagation). Two rules bind the combat model:

1. **Damage is hex-occupancy-resolved.** A Delivery produces a set of **covered hexes**; every
   hostile pawn whose `HexPosition` lies in that set takes the damage. The **aim anchor** (a pawn or
   a hex) is geometry that *orients the shape*, not the list of recipients. The sole exceptions are a
   `Single` lock and `Homing` (which re-resolves its anchor to the moved target at impact) — these
   hit the anchored pawn directly. Delivery is **recursive**: a Payload is a child delivery node
   spawned on impact, anchored at the parent's impact hex, with its own pattern and shape size.

2. **The hex grid is the contract; sprites interpolate; covered hexes are telegraphed one tick
   ahead.** The fixed-tick simulation (ADR-0001) is authoritative on `HexPosition`; the view glides
   between hex states for feel and lags the model by ≤1 tick. To keep hex-occupancy damage *fair*
   under that lag, a delivery's covered hexes (and a pawn's planned move) are drawn on the grid the
   tick **before** they resolve. The player reads danger from lit hexes, never from sprite position.

## Why

Once any delivery is a shape (Beam, Cone, Aoe, Explode), damage *must* be resolved on the grid — a
beam hits whoever stands on its line, not "the locked target." Target-locking only ever described the
`Single` path; generalising it to shapes is incoherent. Hex-occupancy is also what the existing
payload path already does (`ResolvePayloadTargets`), so this ratifies and generalises the real model
rather than inventing one.

Resolving on hexes reopens a model/view desync: a pawn can be clipped by a shape because its *logical*
hex is covered while its *sprite* has glided off. Telegraphing the covered hexes makes the hex — not
the sprite — the legible contract, so the sub-tick lag becomes mere animation over a discrete truth.
This is the Into the Breach model already cited in `Attack Targeting.md` (fully telegraphed,
deterministic targeting on both sides).

## Considered and rejected

- **Commit-on-arrival / destination reservation** (flip `HexPosition` only when the glide lands, so
  occupancy matches the sprite). Rejected: it resurrects the `_reservedHexes` concept ADR-0001 /
  Candidate #5 deliberately deleted, and adds real complexity to buy a ≤1-tick cosmetic alignment that
  telegraphing already makes fair. Revisit only if telegraphing proves insufficient in playtest.

## Consequences

- The root weapon `Fire` (currently hard-locked `Single`) must grow a Delivery Pattern axis; today
  shapes exist only in the Payload path. This is the home for Candidate #5 Decision 2b (the
  delivery-pattern split) and `Cone` (no hex math exists yet).
- `PayloadTargeting` (`Single/Aoe/Line/Self`) is a mis-named Delivery Pattern enum, not a
  target-selection one; renaming/relocating it is owed cleanup.
- "Range" must be split into **Reach** (acquisition distance, the ADR-0001 pawn stat) and a delivery's
  **shape size** (pattern parameter). The two must never share the word.

**Update (ADR-0003, 2026-06-23):** the three owed-cleanup items above are resolved. The weapon grew a
stackable `DeliveryPattern` axis; `PayloadTargeting` was renamed to `DeliveryPattern`; `Cone` was
replaced by `Cleave` (with real hex math). The shape-size knob was *dropped*, not split out — size-free
patterns scale by Reach (the acquisition gate), and only the payload-side `Aoe` keeps a radius. The
`Single` direct-hit special case named above is also gone: damage is now uniformly hex-occupancy. See
**ADR-0003**.

---
tags:
  - ADR
  - Combat
  - Resources
  - Balance
status: Accepted
date: 2026-07-02
---

# ADR-0008 — Mana regenerates during combat, on the same tick as health

**Status:** Accepted (2026-07-02)
**Lifecycle:** Partially implemented — `CombatCoordinator.RegenerateResources` wired, pawn/item
economy retuned in the same pass (2026-07-02); not yet playtested in the Unity Editor.
**Amends:** the undocumented 2026-06-22 stance recorded in `KNOWN_ISSUES.md` ("Mana regen left
unwired on purpose, attack-cost economy") — that line predates this ADR and is superseded by it.
**Companion:** ADR-0005 (resource economy: Cost/Gain-on-hit/Magnitude), ADR-0006 (propagation cost
economy).
**Context:** Mana was left non-regenerating mid-combat as a deliberate "attack-cost economy" choice —
the working theory was that a weapon should cost a *budget* of shots, not a *rate*. In practice this
makes mana a one-way ratchet within a fight: once a pawn's pool empties, it goes **permanently**
silent for the rest of that combat (barring a mana-leech payload or an `OnManaDeplete` Reactor,
neither of which is guaranteed on a starter loadout). Checking the starter weapons against this
model exposes the failure: `manaPool / cost × damage` for every starter weapon undershoots a
full-health target even before the opponent's own health regen is counted. Two pawns trading blows
with starter gear reliably empty their mana before either dies, then sit at a permanent health-regen
stalemate — the concrete bug this ADR fixes.

---

## Decisions

### 1. Mana regenerates on the combat tick, mirroring health's flat-rate model — Accepted

`CombatCoordinator.RegenerateResources()` calls `unit.Stats.mana.Regenerate(unit.Stats.manaRegen,
_clock.TickInterval)` alongside the existing `health.Regenerate(...)` call, for both teams, every
tick, for the duration of combat only (the `CombatClock` only advances while combat runs — no change
to that boundary). The rate is **flat** (a constant amount per second), the same mechanism already
used for health — not a percentage-of-missing curve, not a burst-recharge-after-cooldown model.
*Why:* a one-shot mana budget has no accuracy advantage over a regenerating rate for *balancing*
purposes — either way the designer is tuning "how much damage can this weapon deal before it's
throttled" — but the regenerating rate degrades gracefully (a weapon fired too eagerly gets throttled
to a lower sustained DPS) instead of catastrophically (a weapon fired at all goes fully mute for the
rest of the fight). Flat-rate is the simplest shape, reuses the mechanism already built and proven for
health, and composes predictably with `ManaMax`-scaling Amplifiers (a percentage-of-missing curve
would make max-mana stacking behave unpredictably relative to flat-cost weapons).

### 2. The attack-cost economy is balanced around *sustainable fire rate*, not a fixed shot count, with no extra empty-pool penalty — Accepted

A weapon's long-run sustained attack rate is `min(naturalCadence, manaRegen / cost)`, and its
long-run sustained DPS is that rate × damage. A shot that can't be afforded on its scheduled tick
(`Resource.CanSpend` fails) simply doesn't happen — no partial-cost, no negative mana, and critically
**no additional penalty** (no extended cooldown, no damage/speed debuff) beyond the missed shot itself.
*Why:* the missed shot already compounds naturally (missed damage → missed leech-on-hit → slower
future affordability), which is the intended throttle. A separate "empty mana" penalty state would be
a second, overlapping punishment for the same event — better suited to a future item/pawn-effect
(alternate regen curves, empty-state penalties) than baked into the base mechanic. This is also what
makes Gain-on-hit (leech, ADR-0005) load-bearing during a fight: it visibly raises a weapon's
*sustained* rate rather than just delaying an empty-pool cliff edge by a few shots.

### 3. Pawn mana pool and mana regen are re-baselined: 60→80 max, 5→8/s — Accepted

*Why:* sized so a starter weapon's sustained fire rate isn't crushed to near-zero by the throttle
formula in Decision 2 — pure tuning, playtest-validated per the acceptance-gate exemption, not
re-derived from first principles here. Unaffected by Decision 4 (health compression) — mana pool
sizing is calibrated against weapon *cost*, not pawn health.

### 4. Pawn health is compressed (100→50 max, 2→1 regen/s) to hit a short early-game time-to-kill — Accepted

Weapon damage stays low (Stone 2, Crossblades 5, WoodenSword 3 — unchanged) rather than being scaled
up to compensate; instead the health pool itself is halved, keeping the healthRegen-to-health ratio
constant (~2%/s of pool). *Why:* early encounters should resolve quickly — the game's progression
curve (bigger pawns, bigger loadouts, presumably bigger health pools) is the intended lever for making
*later* fights take longer, not inflated starter weapon numbers. Compressing health over inflating
damage keeps starter weapon numbers legible/low as a deliberate starting point, at the cost of making
`baseHealth: 50` the new round-number baseline every future percentage-based stat gets balanced
against.

### 5. A weapon's cadence is a fixed, unconditional schedule — a missed shot doesn't shift the schedule — Accepted

Documents existing `CombatClock` behavior (`CombatClock.cs:48-67`) as intentional, not incidental:
`OnTick` fires strictly every `tickInterval` regardless of whether the previous tick's callback
actually fired (i.e. regardless of `CanFire` outcome). A weapon that fails to fire due to insufficient
mana does **not** get an extended cooldown, and does **not** get an early retry the instant mana
crosses the affordability threshold mid-interval — the next check is the next regularly-scheduled
tick. *Why:* this is what makes Decision 2's `min(cadence, regen/cost)` throttle formula hold cleanly
— missed attempts are independent per-tick checks against a fixed grid, not compounding cooldown
debt or granted early windows.

## Worked example

Pawn baseline (`Player.asset`/`Enemy.asset`): `baseHealth 50`, `baseHealthRegen 1`, `baseMana 80`,
`baseManaRegen 8`.

| Weapon | dmg | cost | natural cadence | net cost (after leech) | sustained rate | sustained DPS | net vs. healthRegen(1) | TTK |
|---|---|---|---|---|---|---|---|---|
| Stone | 2 | 4 | 2.5/s | 4 | min(2.5, 8/4)=2.0/s | 4.0 | 3.0/s | **~17s** |
| Crossblades | 5 | 14 | 1.0/s | 12 (leech 2/hit) | min(1, 8/12)=0.667/s | 3.33 | 2.33/s | **~21s** |
| WoodenSword | 3 | 12 | 1.4/s | 9 (leech 3/hit) | min(1.4, 8/9)=0.889/s | 2.67 | 1.67/s | **~30s** |

Every starter weapon now clears the health-regen floor (Decision 2 + 4 together — sustained DPS
exceeds `healthRegen`, so `Resource.CanSpend`'s throttle never fully silences a weapon the way the
old one-shot budget did per Decision 1's Context). Before this ADR, Stone's *entire* mana pool could
deal at most 40 total damage against a 100 HP target — not enough to kill it even once, let alone
account for the target's own regen; that failure mode is gone. WoodenSword is the outlier at ~30s
(its damage-to-net-cost ratio is the weakest of the three) — flagged as a playtest watch-item rather
than hand-tuned further here (Decision 4's rationale explicitly defers precision to playtest).

**Approximation note:** the `min(cadence, regen/cost)` formula is continuous-math, not a verified
discrete-tick simulation — the real combat tick is fixed at 0.1s (`CombatCoordinator._tickInterval`)
and `CanFire` is checked once per weapon cadence tick, not every combat tick. At these numbers the
discrete stepping is fine-grained enough that the approximation should track closely, but this has
not been hand-simulated tick-by-tick. Per the acceptance-gate's tuning exemption, this is left for
the Editor playtest to catch rather than hand-verified here.

## Considered and rejected

- **Keep mana non-regenerating; raise weapon damage-per-mana until the one-shot budget alone exceeds
  a full health pool.** Rejected: papers over the same cliff edge rather than removing it — any
  future pawn with more health, or any fight with mutual chip damage between shots, can still reach
  "both sides silent, health-regen stalemate forever"; a throttle degrades gracefully where a
  one-shot budget cannot degrade at all past zero. (Also separately rejected as the *pacing* fix in
  favor of Decision 4 — raising damage was considered again there and rejected in favor of
  compressing health, to keep starter weapon numbers low per design intent.)
- **Keep mana non-regenerating; treat `OnManaDeplete` Reactors / mana-leech payloads as the intended
  answer to empty-pool lockout.** Rejected as the *sole* answer: makes baseline (unitemized) combat
  systematically unresolvable and makes early-game fights depend on drawing the right item. Leech and
  `OnManaDeplete` remain valid, additive build-arounds under Decision 2 — they raise an already-finite
  sustained DPS rather than being the only thing standing between a pawn and permanent silence.
- **A harder "empty mana" penalty state** (extended cooldown or debuff once mana hits exactly 0).
  Rejected for the base mechanic (Decision 2) — a second overlapping punishment for the same missed-
  shot event; left as a future itemizable/pawn-effect surface instead.
- **Percentage-of-missing or burst-recharge mana regen curves**, instead of flat-rate. Rejected for
  the base mechanic (Decision 1) — flat-rate reuses the proven health mechanism and composes
  predictably with `ManaMax` scaling; alternate curves are deferred as a toggleable item/effect
  surface, not baked into the default.

## Deferred (designed, not built)

- **Alternate mana-regen curves** (percentage-of-missing, burst-recharge, "second wind" thresholds)
  as an itemizable or pawn-effect toggle layered on top of the flat-rate default (Decision 1).
- **A harder empty-mana penalty state**, if playtest shows the pure-throttle model (Decision 2) feels
  too forgiving — same surface as above, an opt-in effect rather than a base-mechanic change.
- **Out-of-combat mana regen / rest mechanic** (`KNOWN_ISSUES.md`: "implement resource gen first and
  think of giving a bonus while not in combat") — still open, unaffected by this ADR; this ADR only
  covers the combat-tick case.

## Open questions

- The discrete-tick approximation in the worked example is unverified against the real 0.1s
  `CombatCoordinator` tick — left for Editor playtest to surface any divergence rather than
  hand-simulated here (see Worked example's Approximation note).
- WoodenSword's ~30s TTK is noticeably longer than Stone/Crossblades (~17–21s) — watch in playtest;
  no further hand-tuning applied here per Decision 4's explicit deferral to playtest.

## Consequences

- **Positive:** combat now has a guaranteed-finite outcome under the intended attack-cost economy;
  Gain-on-hit (leech, ADR-0005) becomes a visible sustained-DPS lever instead of a shot-count
  extender; mana pool/regen and health pool/regen are now four independent, legible tuning knobs;
  `CombatClock`'s fixed-schedule cadence (Decision 5) is confirmed as the right existing behavior
  rather than an accidental one.
- **Negative / debts:** invalidates the "attack-cost economy" framing in `KNOWN_ISSUES.md` line 4 —
  marked superseded by this ADR, not rewritten. The `OnManaDeplete` Reactor's design weight shifts
  from "the fix for permanent silence" to "an optional burst-recovery build-around," and — because
  mana now hovers near-empty in a throttled steady state rather than emptying once — `OnManaDeplete`
  will fire *more* often than under the old model; no authored Reactor asset currently uses it, so
  nothing breaks today, but its cost-modifier tuning should account for this if/when one is authored.
  **Flat pool-modifying item passives are proportional to the pawn baseline they were authored
  against** — halving `baseHealth` (Decision 4) required rescaling every flat `LifeMax`/`LifeRegen`
  item passive in the same pass (Amplifier "Cap" +10→+5 LifeMax, `Converter_Resource` +15→+8,
  `Converter_AnchorSelf` +10→+5, `Converter_Friendly` +3→+1.5 LifeRegen) to preserve their intended
  relative weight; any future baseline change owes the same audit. Not yet playtested — `Lifecycle`
  will move to `Implemented` once verified in the Unity Editor.

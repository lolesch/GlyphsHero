---
tags:
  - Item
  - Attachment
  - ChainRoot
  - Inventory
---
# Definition

> [!tldr]+ Description
> Replaces the weapon's timer with an external combat event. Weapon only fires when the event occurs

> [!quote]- Purpose - *Why is this essential?*
> Bridges [[Item Chaining]] and the hex unit system - combat directly empowers individual weapon chains.

> [!check]- Reward - *What is the gain?*
> Attack rate can greatly exceed normal timer rate if the triggering event is frequent.

> [!warning]- Risk - *What are the punishments*
> Low combat impact / loss of control - Total dependency on external conditions
> [[Weapon#Input Economy|input costs]] drain resources

> [!fail]- Opposition - *What counters this?*
> Bypassing the event / event suppression

> [!error]- Polarity - *What increases its weakness?*
> Make common events rare on Reactors, 
> High [[Weapon#Input Economy|input costs]] per attack

> [!example]- Progress - *What is the goal*
> Opt-in frequency optimization.
> Overwrite timer-limitation

> [!info]- Depth - *Where are the synergies*
> Overrides [[Weapon]] timers
> Negates frequency penalties from [[Shifter]]
> Hex Grid layer feeds inventory output - positioning, team comp become relevant 

> [!tip]- Appeal - *Does it help the game*
> Bridging the systems - Chain → hex map → back to chain

---

### Cost lever — event frequency taxes the fire

A Reactor doesn't fire for free: it applies a **cost multiplier scaled by how common its event is**
(common event → bigger multiplier), so cheap high-frequency triggering is taxed and a weapon on a Reactor
still can't fire without the (scaled) resource to spend ([[0006-payload-propagation-cost-economy|ADR-0006]]
§6). Mechanically the Reactor is just a **cost `Modifier`** on the weapon's base cost — the same kind of
thing a Payload is, differing only in *where it sits* (the root/trigger) and *what sets its value* (event
rarity). This is the Reactor's balance lever; it is **not** a threshold/predicate.

### Watch-conditions (deferred — the Trigger-axis condition model)

The patterns below are **watch-conditions** — they observe a value and fire when it crosses. They are the
home of the old `ConditionType` watch predicates (`ResourceBelow/Above/Full/Depleted`), which ADR-0006
removed from the Payload because they were never a payload concern. They belong here, on the Reactor /
Trigger axis, and are **deferred to a future Trigger-condition ADR** (the `ReactorConfig.ConditionType`
field is stubbed out, waiting for it).

**Every X seconds during [state]** _(Backpack Battles)_ — a timer that only runs while a condition is active. Creates a build goal: enter the state, then the Reactor fires.

**Opponent reaches X [resource]** _(Backpack Battles)_ — watches an enemy stat. Opens poison-stacking as a chain trigger.

**Before defeat** _(Backpack Battles)_ — fires once when this unit would die. Natural fit for a last-stand payload weapon.

---

## Deferred

**Counter-based triggers** (every N hits/kills) — a watch-condition on the **Reactor** (the Payload gate is now purely economic, ADR-0006). Explore as part of the deferred Trigger-condition ADR above.

**Response delay** — fire X seconds after the condition is met. Underexplored in all reference games. Revisit when chain resolution timing is designed.
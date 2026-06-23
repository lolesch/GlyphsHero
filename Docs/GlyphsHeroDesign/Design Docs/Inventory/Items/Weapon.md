---
tags:
  - Item
  - ChainRoot
  - Inventory
aliases:
  - Weapons
---
# Definition

> [!tldr]+ Description
> The source of all attacks - modified via [[Item Chaining]]
> See [[Payload]] if used in another weapons chain -> Tradeoff between usage

> [!quote]- Purpose - *Why is this essential?*
> Without a weapon a pawn does nothing -> no combat impact.

> [!check]- Reward - *What is the gain?*
> Combat impact via predictable attacks. Apply damage, status effects and terrain changes, gain resources

> [!warning]- Risk - *What are the punishments*
> High resource usage with low combat impact.

> [!fail]- Opposition - *What counters this?*
> High health and defense layers

> [!error]- Polarity - *What is its weakness?*
> Attacks are gated by conditions - slow timers, low resource pool, high resource drain

> [!example]- Progress - *What is the goal*
> match game difficulty, overcome enemies

> [!info]- Depth - *Where are the synergies*
> The entry point of [[Item Chaining]] - all other items attach to it. 
> Synergizes with itself (payload mode). 

> [!tip]- Appeal - *Does it help the game*
> Necessity for pawn combat, Entry point for item chaining (the core of glyphs hero)

---

# Starter Weapon

Each unit arrives with a unique starter weapon that defines its combat character. Replaceable but not required to replace — gives new players a scaffold and experienced players a choice.

---

# Trigger

*When/how often* the weapon fires (renamed from "Delivery Mode" to end the "delivery" overload — see [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]] §5). Fires on its own timer by default; a [[Reactor]] can replace the timer with a combat event, and a [[Shifter]] trades attack speed. Resource costs are paid each time the weapon fires — a threshold gate.
## Weapon Stats
### Input Economy

define when and at what cost the weapon attacks.
A weapons internal timer based on Attack Speed can be overwritten by a [[Reactor]], forcing it to instead fire on external [[Combat#Combat Events|Combat Events]].

- Attack Speed
- Life Cost
- Mana Cost
- Proc Chance -> Reliability of secondary effects — payload firing chance, gen proc chance.
### Output Economy

define what the attack produces.

- Damage
- Life On Hit / Leech
- Mana On Hit
- StatusApplication

> **Range is NOT a weapon output stat** — see [[0001-range-movement-and-combat-tick|ADR-0001]] §2.
> Range moved to the **pawn**. Weapons no longer carry a range identity; they vary by payload, shape,
> and economy.

#### Reach (a pawn stat)

**Reach** lives on the **pawn**, not the weapon — a single, uniform acquisition gate and the pawn's
archetype identity. **"Melee" vs "ranged" is just Reach = 1 vs Reach > 1** (ADR-0004 §2); there is no
per-weapon reach and no range-scaling/range-fixed split (ADR-0001 §2b is withdrawn). What a *weapon*
still controls is the **delivery pattern** (via [[Converter]]) — a Reach-3 pawn can fire a `Line`,
`Cleave`, etc., but the pattern never changes how far it stands. Reach is capped, not infinitely
scalable, and bought only via passive item stats that cost grid space.

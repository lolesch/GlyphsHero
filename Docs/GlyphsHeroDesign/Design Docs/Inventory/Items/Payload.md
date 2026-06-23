---
tags:
  - Item
  - Attachment
  - Inventory
---

**Condition:** proc chance and resource cost are the default gates. Chain propagation stops if unmet. 
A payload must enable at least one behavior that cannot exist if both weapons are used independently.
# Definition

> [!tldr]+ Description
> Fires conditionally when [[Item Chaining|Chain Propagation]] reaches it.
> Enhances an attack with behavior that cannot exist if used as a [[Weapon]] -> tradeoff

> [!quote]- Purpose - *Why is this essential?*
> Bridges from attacks into hex grid layer. Customize attacks to maximize synergies. 

> [!check]- Reward - *What is the gain?*
> Adds status effects and terrain impact to attacks. Combinations impossible when both weapons fire independently. 

> [!warning]- Risk - *What are the punishments*
> [[Payload#Affinity Tags|Affinity Tags]] mismatch lowers the impact.
> Stopping [[Item Chaining|Chain Propagation]] by unmet condition.
> Bad resource management 

> [!fail]- Opposition - *What counters this?*
> [[Payload#Affinity Tags|Affinity Tags]] mismatch
> Weapon activation penalties

> [!error]- Polarity - *What increases its weakness?*
> Small pools with shared [[Payload#Affinity Tags|Affinity Tags]] -> more mismatch.
> High conditions to meet. 
> High activation penalties. 

> [!example]- Progress - *What is the goal*
> Shape combat in unexpected ways.
> Combat complexity and specialization

> [!info]- Depth - *Where are the synergies*
> Relies on [[Item Chaining]] for optimal setup. 
> Scripted Synergies with [[Weapon]] are planed.

> [!tip]- Appeal - *Does it help the game*
> Core fantasy the [[Item Chaining]] is built around.
> Bridging from Inventory to Hex Grid -> predictable tactile layer

---

# A payload is a child delivery node

A payload doesn't have its own bespoke axes — it is a **child [[Attack Targeting#Delivery|delivery node]]** that recurses through the full attack model (Target Selection × Delivery Pattern × Layers × Affinity × Anchor), triggered on the parent's impact. See [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]] §4.

The old "propagation behaviors" are just child configurations:

| Behavior  | = child configured as                                              |
| --------- | ----------------------------------------------------------------- |
| `Explode` | a child with the `Aoe` pattern                                     |
| `Return`  | a child with anchor = origin                                       |
| `Split`   | child count = N parallel — requires a [[Splitter & Merger\|Splitter]] |
| `Chain`   | nested payloads detonating in sequence (no exclude-already-hit)    |
| `Fork`    | **removed** (redundant with Split)                                 |
| `Pierce`  | **moved** to a Delivery Layer (it's about *hitting*, not on-hit)   |

# Effect Axes (what a payload adds beyond geometry)

| Axis                | Dimensions                                                                      |
| ------------------- | ------------------------------------------------------------------------------- |
| **Temporal** (when) | Instant · Delayed · Repeating · Duration                                        |
| **Effect** (what)   | Status effects · Positioning (push/pull/stun) · Terrain changes                 |

---
# Affinity Tags

Every weapon is *good at expressing certain payloads, awkward at others.* Measured by tag overlap — matching tags raise expression strength; mismatches lower it (threshold model, think DIII set bonuses).

Tags serve dual roles: on a **weapon** they declare capability; on a **payload** they declare modifier requirements.

| Tag group | Examples |
|---|---|
| Delivery | `melee` `ranged` `projectile` `beam` |
| Spread | `path` `aoe` `pierce` `chain` |
| Interaction | `hit` `dot` `status` `control` `terrain` |
| Constraint | `los` `random` `entity` |

---
# Design References
- **PoE CoC / CwDT** — linked spell fires conditionally when reached, never freely
- **Noita trigger spells** — payload doesn't become a second free-firing wand


---
tags:
  - Item
  - Attachment
  - Inventory
---
# Definition

- **Commitment** - damage/target type choice may be strong or weak against the encounter

> [!tldr]+ Description
> Reclassifies the output type of the nearest upstream weapon

> [!quote]- Purpose - *Why is this essential?*
> Opens interactions and on the Hex Grid; turns a generic weapon into an encounter-specific tool.

> [!check]- Reward - *What is the gain?*
> Access to element interactions, spread patterns, and resource routing unavailable through raw stats.

> [!warning]- Risk - *What are the punishments*
> Hard commitment - the converted type may be weak or irrelevant against the current encounter.

> [!fail]- Opposition - *What counters this?*
> Converted type immunity

> [!error]- Polarity - *What increases its weakness?*
> Combat with moving parts, type doesn't fit anymore

> [!example]- Progress - *What is the goal*
> Build specialization towards interaction synergies.
> Adopt to the current encounter

> [!info]- Depth - *Where are the synergies*
> Attack expression takes place on Hex Grid - type conversion tailors interactions.

> [!tip]- Appeal - *Does it help the game*
> The _surgeon_ play: precision typing for maximum exploitation of enemy vulnerability.

---

## Signal Types and Conversion

The Converter is the **type-reclassifier** — it changes *what kind* a signal is on any axis, never *how much* (that's the [[Amplifier]]) and never *the trade between stats* (that's the [[Shifter]]). See [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]] §1.

- **Typed signals** exist for: damage type, **target strategy**, **delivery pattern**, resource type, optionally trigger **event** type.
- Converters change typed signals locally — nearest upstream action node only.
- Weapons can carry types/Tags that get converted — converting a 'heavy' hammer to a 'swift' one.

**Converter types:**

| Axis                 | Conversion                            | Notes                                                  |
| -------------------- | ------------------------------------- | ------------------------------------------------------ |
| Damage type          | physical → fire / ice / poison / …    | Changes damage scaling and interactions                |
| Target strategy      | Nearest → LowestHP → …                | The [[Attack Targeting#Target Selection]] axis (was mis-assigned to the Shifter) |
| Delivery pattern     | Single → Line → Cleave → …            | Hex coverage geometry — *not* "shape size"             |
| Resource type (gen)  | mana-on-hit → health-on-hit           | Changes what `ResourceGenOnHit` fills                  |
| Resource type (cost) | mana cost → health cost               | Changes what `ResourceCost` spends                     |
| Trigger event        | on-hit → on-crit                      | Reclassifies an *existing* event type; never frequency. Borders the [[Reactor]] (which *installs* an event trigger) |

> **Not the Converter's job:** firing *frequency*/cadence (Weapon timer / [[Reactor]] / [[Shifter]] speed), and stat *magnitude* ([[Amplifier]]). The old "delivery mode: instant → projectile → accumulated burst" row conflated cadence with coverage — removed.

## Implementation status (2026-06-24)

Built for the three axes that exist as data on `WeaponStats`: **Delivery pattern**, **Affinity**, **Anchor**. A `ConverterConfig` picks one `ConverterAxis` and the target value; `WeaponStatResolver` replaces that axis (last-wins) on the nearest upstream weapon's resolved stats. Deferred for lack of a data system: **damage type** (untyped), **target strategy** (not data-driven yet), **resource type** (mana-only cost/gen), **trigger event**. See [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]] consequences.

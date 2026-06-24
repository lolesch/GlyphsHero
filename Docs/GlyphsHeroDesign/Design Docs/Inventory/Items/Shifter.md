---
tags:
  - Item
  - Attachment
  - ChainRoot
  - Inventory
---
# Definition

> [!tldr]+ Description
> trades across [[Weapon#Weapon Stats|Stat Economy]] - input- for output-stat or vice versa.

> [!quote]- Purpose - *Why is this essential?*
> Allows build specialization - the only item that interacts with both stat economies.

> [!check]- Reward - *What is the gain?*
> Balance between input and output stats.
> Controlling your build - shifting towards specific setups.

> [!warning]- Risk - *What are the punishments*
> Shift towards suboptimal outcome.
> 

> [!fail]- Opposition - *What counters this?*
> Offered shift might be the opposite of the preferred conversion.

> [!error]- Polarity - *What increases its weakness?*
> High penalties for low benefit shift -> overall stat decrease
> Chaining opposing shifters doe's not add value

> [!example]- Progress - *What is the goal*
> Build Specialization and condition manipulation across [[Weapon#Weapon Stats|Weapon Stat Economy]]

> [!info]- Depth - *Where are the synergies*
> 'Free' stat boosts if correctly combined with [[Reactor]]
> If one can manage to make a build out of penalties, shifters add huge value.
> Synergizes with builds that focus on one specific stat like ProcChance or AttackSpeed

> [!tip]- Appeal - *Does it help the game*
> Risk/reward axis that rewards knowing and controlling your build / [[Item Chaining]].

---

# Trades

One firing stat against one output stat. Every bonus costs something on another axis.

> **Scope:** the Shifter is *only* the economy-trade item. It does **not** modify Target Selection — that is the [[Converter]]'s job (ADR-0004 §1 corrects the earlier mis-assignment). Magnitude is the [[Amplifier]]; type reclassification is the [[Converter]].

| Trade                          | Meaning                          |
| ------------------------------ | -------------------------------- |
| `AttackSpeed` ↓ → `Damage` ↑   | Slow but hits hard               |
| `AttackSpeed` ↑ → `Damage` ↓   | Fast but soft - generator build  |
| `Cost` ↑ → `Damage` ↑          | Expensive but powerful           |
| `Cost` ↓ → `Damage` ↓          | Cheap and soft                   |

> The Shifter trades the **magnitude** of a weapon stat (ADR-0004 §1). It trades the **`Cost`** magnitude
> (one pool, ADR-0005 §4), *not* gain — gain left the weapon to become a per-hit `ResourcePayloadEffect`
> (ADR-0005 §3), so there is no `ResourceGenOnHit` weapon stat to trade against. Scaling the *effect* (leech
> %) is the deferred effect-magnitude axis, not the Shifter.

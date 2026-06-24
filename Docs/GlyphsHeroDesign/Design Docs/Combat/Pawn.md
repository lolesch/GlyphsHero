---
aliases:
  - Pawns
---

Pawns occupy hexes and have positional interactive effects with neighbors. 
Each pawn has a 2D grid [[Inventory]] where item placement defines a pawns attacks.
# Pawn Identity

A pawn's identity is defined by three layers working in combination:

1. **[[Pawn#Weapon Chain|Attacks]]** - moment-to-moment combat expression
	- changes from combat to combat via loot assignment.
	- Attack Payloads alter the other layers with status effects and terrain modifications.
2. **[[Pawn#Aura (PawnEffect)|PawnEffect]]** - unit-to-unit hex positioning
	- changes during combat via movement.
3. **[[Pawn#Terrain Interaction|Terrain]]** - unit-to-world effects driven by what the pawn stands on
	- changes during combat via movement / terrain changes.

These layers are parallel, not hierarchical. A pawn feels like something because all three are present simultaneously. Replacing a weapon chain redirects combat expression but does not erase the pawn's positional identity.

### Weapon Chain
Defined by [[Item Chaining]] are in inventory. The [[Weapon#Starter Weapon|Starter Weapon]] is the default expression of the pawn's combat character — it scaffolds new players and gives experienced players a known baseline to build from or contradict.

### Aura (PawnEffect)
Passive effects that affect nearby allies or enemies. Currently defined by a hex shape and a modifier. The player builds this layer over a run via opt-in pickups — permanent upgrades the player can take or skip, similar to Slay the Spire relics. This layer changes at run pace, not combat pace.

### Terrain Interaction
Effects triggered by what the pawn stands on.

Terrain and Aura are intentionally coupled — terrain can enable or gate Aura conditions. 
See [[Weapon#Weapon-Payload Affinity]] as reference for interaction strength.

Examples of the design space:
- Standing on lava gives a damage bonus
- Aura effect only applies to enemies currently on fire
- Standing on lava sets nearby enemies on fire, enabling the Aura condition
- Moving off ice removes a speed buff

This coupling means positioning is not just "who is adjacent to whom" — it also asks "what am I standing on and does it activate my Aura."

---

## Positioning as a Tactical Tool

Every unit placement has three simultaneous optimization reads:

| Read | System | Question |
|---|---|---|
| **Team composition** | Aura | Who do I buff or debuff by being here? |
| **Terrain affinity** | Terrain ↔ Aura | Does this tile enable my effects? |
| **Weapon range** | Item chain | Am I close enough to hit what my chain wants? |

These three pulls on a single placement decision is where the tactical puzzle lives.

---

## Pacing Layers

| Layer | Change pace | Change driver |
|---|---|---|
| **Terrain** | Per map / zone | World state, slow progression |
| **Aura** | Per run | Player opt-in pickups |
| **Weapon chain** | Per combat | Loot drops |

Each layer has its own investment curve. Terrain is read and reacted to. Aura is authored slowly. The weapon chain is rebuilt rapidly.

---

## Combat Structure

Two-phase combat — **decided** (see [[0001-range-movement-and-combat-tick|ADR-0001]] §1):

**Phase 1 — Positioning (turn-based):** Player moves units, reads terrain, adjusts team composition. This is the deliberate planning layer that makes terrain and Aura legible and actionable. **The strategic weight of positioning lives here** — Resolution mostly executes what Placement set up.

**Phase 2 — Resolution (real-time):** Weapon chains fire, Reactors trigger on events, damage resolves. Runs on a **fixed combat tick** (`CombatClock`), decoupled from the frame rate, so the simulation is deterministic and the view interpolates between ticks. This preserves the item chain design intact.

This model avoids the conflict between turn-based repositioning and real-time weapon chain firing by giving each a separate time domain. Reactors are naturally compatible with turn-event framing.

---

# Build Strategies


- **Starter weapon** - needs to express each pawn's Aura identity clearly

## Tank
getting hit is part of the strategy
- convert damage taken into X
- reflect damage
- OnGettingHit reactor

## AttackSpeed
high OnHitEffect stacking
- more hits = more on-hit effects (e.g. leech via `ResourcePayloadEffect`, ADR-0005)
- can stack effects
- can fuel/enable other strategies

## Burst
high resource usage with high downtime
- counters flat mitigation
- if precharged, huge upfront impact on combat

## Glass Cannon
oneshot before getting hit
- relies on setup or support
- high risk/reward

## Healer / Support
low DPS, huge battlefield impact
- healer is actually a subclass/specialization
- control mage is also a support and so on...

---

## TBD 

- **Aura upgrade pool design** — opt-in relic-style pickups not yet designed



# Movement

**Decided — see [[0001-range-movement-and-combat-tick|ADR-0001]] §3–§7.**

### Range is a pawn stat
A pawn has one **range** stat (its archetype identity — sniper vs. brawler), the **ceiling for
range-scaling deliveries** (Projectile/Beam/Arc). Range-fixed deliveries (Adjacent/Dash) stay range-1
regardless. Range is **capped, not infinitely scalable**, and bought only via passive item stats that
cost inventory-grid space (a real tradeoff) — not pumped by Amplifiers. Shape is still Converter-driven,
so a range-3 attack can be a beam, cone, etc. Weapons no longer carry a range identity (they vary by
payload/shape/economy).

### Default movement — monotone closing
- A pawn closes to the **minimum effective reach across its *active* weapons**, so all of them can fire
  (the player authors the engagement profile through the weapon mix).
- **No kiting/retreat in v1** — monotone closing only. This removes mutual-kite oscillation deadlock, so
  no stalemate-breaker is needed yet.
- Contested hex → **closest-to-target wins** (stable-id tiebreak). Blocked by allies/terrain → **idle and
  re-evaluate** next tick (allies are passable-but-costly; destination must be empty). A walled-off unit
  idling is legible punishment for bad placement — *but must be telegraphed as blocked*.

### Deferred (designed, not built)
- **Kite/retreat** as an opt-in movement-strategy item — must ship paired with a stalemate breaker
  (fatigue / advance pressure).
- **Cooperative single-hop sidestep**: a blocked pawn asks a blocking ally to step aside, but only to a
  hex equally valid on *all three* placement reads (range + Aura + terrain) — never cascading (a cascade
  is the heavy planner). Else idle.
- Pull/push effects: pushing into occupied/invalid hexes adds a stun; or chain-push a whole line.



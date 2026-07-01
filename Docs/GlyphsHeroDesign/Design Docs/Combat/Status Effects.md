
> [!warning] Unbuilt
> No status effect system exists yet. `StatusPayloadEffect` is a data stub with no logic; combat code
> explicitly logs "StatusPayloadEffect not yet wired — status system pending." Terrain types are defined
> but inert (no gameplay reads them). This whole document is forward-looking design.

Status effects scale better via payload than via delivery mode.
Statuses scale primarily through payloads.

each effect should have a pawn impact as well as a terrain impact.
or in other words, terrain can apply effects and can be transformed based on effects. -> ice gives speed but applies freeze, can be molten with strong burn effects. 

Example statuses:
- Burn → spreads
- Freeze → control, shatter
- Shock → chaining
- Poison → stacking, area denial
- Bleed → movement-triggered damage
- Root → control amplifier
- Flux → chaos/randomness

---
## Terrain System

Terrain modifies and amplifies status effects.

Types:
- Burning
- Frozen
- Toxic
- Conductive
- Obstructed

---

## Status ↔ Terrain Interactions

Examples:
- Burn + Toxic → explosion
- Shock + Conductive → chain spread
- Freeze + Burn → cancel
- Root + Push → damage conversion
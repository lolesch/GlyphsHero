
Use unique items to modify [[Pawn#Aura (PawnEffect)|PawnEffects]] (crafting currency like in poe)
- "use" these items by right-clicking and then left-clicking the pawn the item should be used on. This will destroy/remove the item from inventory and permanently apply it's pawnEffectModifier to that unit. <- relic-like upgrade.
	- maybe items could be broken down into currency for further customization/crafting
- Or it is the unique stat mod and just sits in the inventory (unchained only?)
   
defensive layers - avoid, reduce, regen
- have mechanics that are effective against hard-hitting (%reduction) and others against many attacks (flatReduction)

# Block
Armor and Shields can block attacks, without them, no block. (equipment items - not chainable?)
a shield could reflect damage back to the attacker
a legendary shield could reflect all damage of melee weapons. This would be an interesting enemy to overcome -> strategy needed.

Shields against melee, Armor against ranged?

# Stats

The pawn / [[Pawn#Aura (PawnEffect)|PawnEffect]] could have some form of [[Payload#Affinity Tags|weapon handling]] that adds modifiers to equipped weapons. More defined Weapon types add a layer of customization and balancing.

pawn could have general weapon stats, so they would apply to all equipped weapons, like
- ResourceCostReduction
- CDR/attackSpeed 
	- adrenaline/focus (status effect) grants attack speed per stack
		- is applied by combat events such as getting flanked, being hit, executing someone and so on.
- damageScaler
- ...

- [ ] **Max Resource vs Regeneration Rate**
	- Big pool vs fast recovery = different playstyles
- [ ] shield as defensive layer (energy shield)
	- break it, ignore it, reduce its effectiveness and so on
- [ ] Resource Overflow should grant shield for the other resource
	- so ManaOverflow creates health shield and vise versa
	- this could rarely intentionally be flipped or manipulated
- [ ] stat conversion
	- convert % damage to instead drain enemies mana or so
	- convert % damage into resourceGain (leech)
	- convert missing health (not % but flat - higher pools benefit) to X
- [ ] conditionals
	- bonus damage at full/low resource
	- bonus X for attacks that apply burning...
	- +X% per consecutive hit

---

# Item Ideas

have an item with negative stats both in chained and unchained state, so it is a burden to keep/carry it but it has a synergy counterpart that converts the negative into power

+1 MaxMana per hit and +1% damage per CurrentMana -> scaling mana build but requires scaling mana regen/instant refill to be worth

**Mirror shard** - that (as payload) 
- reverts the chain resolution, so that it goes back through all amplifiers and adds the weapon itself as payload to its own firing 
	- but also blocks the mirrored inventory slots? 
	- Or is simply a large item

sacrificia/ceremonial knife
- targets self, deals little DMG but offers ... yeah, what?

---

## Scripted Synergies
Items can highlight scripted interactions, like 'Backpack Battles' merge function. 

Rock + Whip = Sling
- highly increased range but decreasing accuracy over distance
	Rock has a weak payload, but as a Sling, that payload is much stronger

---

# Achievements

"Hoarder" - fill the entire inventory with 1x1 items
"Chainer" - fill the entire inventory with chained items
"Chain Master" - fill the entire inventory with one chain

---

# Pawn

think about having all pawn stats in the inventory, so the entire health globe/pool is defined by what's on the grid. check other stats too.
- if the item pool is an item, the same chaining could apply

## Pawn Info Card (Magic-Card style)

Show a pawn's info like a trading card (MTG-style layout):
- top half: portrait art + name + core stats (health/resource pools, damage, etc. — same numbers as today's tooltip, just laid out card-like instead of list-like)
- bottom half: "rules text" box listing the pawn's resolved attacks as bullet-pointed sentences, not raw numbers — e.g. "Strikes the nearest enemy for 12 damage" rather than a stat table. Reads like a card ability, generated from the resolved `ItemChain`s (root/weapon/modifiers) rather than authored per-pawn.
- the inventory grid itself is probably *not* part of the card — likely shown alongside/below it as its own panel, since the grid is an interaction surface (drag/drop, chaining) and doesn't fit the card's fixed-layout, read-only framing.

Open questions: how much of the bullet text is templated vs. hand-authored per weapon type; whether this replaces or supplements the existing tooltip system (ADR-0010); where portrait art comes from (placeholder vs. real per-pawn art, which is a content cost, not just a UI one).

## Debuffs Targeting Inventory, Not the Pawn

Instead of (or in addition to) debuffing a pawn's stats directly, a debuff could target the pawn's `TetrisContainer`/inventory:
- an enemy with many amplifiers suffers a flat penalty scaled by amplifier count (e.g. "-1 to all amplifiers") — punishes amplifier-heavy builds specifically, rewards diversifying or going weapon-heavy.
- a debuff that picks a random occupied slot and **freezes** whatever's there for the pawn's next attack — the frozen item is excluded from chain resolution for that attack (as if temporarily removed from the grid), so a frozen root/weapon could disable an entire chain, while a frozen modifier just weakens one.

This composes with the "stats live in the inventory" idea above — if health/resource pools are themselves grid items, the same targeting mechanic could threaten survivability directly, not just damage output. Needs a design-gate pass before implementation: freezing a chain's root/weapon has much higher blast radius than freezing a modifier, and "excluded from the chain" needs a precise definition against `ChainResolver.ResolveTopology` (does a frozen connector item still occupy grid space and block adjacency, or does it act fully absent?).

---

# Loot Agency & Two Combat Modes (2026-08-20)

Current state: `LootPhase` (`Assets/Code/Runtime/Core/LootPhase.cs`) generates scripted/random
items straight into the stash — the player reviews and hits Continue, but never *chooses*
anything. There's no shop, no currency, no reroll. Compared to a game like Backpack Battles,
where the between-round shop is the primary lever for shaping a build, this leaves too little
choice at exactly the moment choice should matter most.

The idea traces back to two different genre references pulling in different directions:
- **ARPG/Backpack Battles lineage** (current implementation): heavy pre-planning, assemble chains
  between fights, then let combat play out hands-off.
- **Horde-survivor lineage**: mobs drop gear *during* combat, no pre-planning safety net — the
  player adjusts item chains live, under pressure, with incomplete information.

These could coexist as two distinct **combat modes** rather than one replacing the other, and
possibly as a property of the encounter (`EncounterConfig`) rather than a global setting —
e.g. a swarm encounter forces real-time Survivor-style adjustment, a boss/elite encounter rewards
long-horizon Planner-style prep. Each mode gatekeeps differently depending on enemy composition.

Adjacent design threads this opens, not yet explored:
- **Shop economy** — what currency gates rerolls/purchases; whether items can be sold back;
  whether reroll cost scales.
- **Real-time editing vs. the combat clock** — in Survivor mode, does editing the grid mid-fight
  cost time or expose the pawn to hits? This reaches into `CombatCoordinator` and the
  `CombatClock` (see architecture-review #7, already implemented), not just `LootPhase`.
- **Pairs with the inventory-targeting-debuff idea above** — Survivor mode's live-pressure framing
  fits naturally with enemies that disrupt the grid mid-fight instead of just pawn stats.
- **Pacing/difficulty axis** — Planner rewards optimization depth, Survivor rewards adaptability;
  could become two campaign branches or a difficulty axis instead of pure encounter flavor.

This is bigger than a one-line idea — it implies a new "combat mode" concept touching
`GamePhaseController`, `LootPhase`, and `EncounterConfig` — but it's nowhere near decided.
Needs real design-gate treatment (two-way door: it would contradict/extend the current
Placement→Combat→Loot loop that ADR-0001 and friends assume) before anyone builds toward it.

---

# Sigil Visualization (from Glyphs/Sigil_Design_Handoff.md audit, 2026-07-02)

Doc-vs-code sweep of the `Glyphs/` folder turned up two gaps worth floating, not deciding:

- **Composite sigil renderer.** [[Sigil_Design_Handoff]] specs a full visual language (trigger-line
  character, weapon center mark, orbiting amplifiers, payload offset) but nothing renders it —
  items still show one static `Sprite Icon`, and the chain's reading order only exists as *text*
  in the tooltip. `ChainResolver` already computes everything the renderer would need (root,
  weapon, ordered modifiers, payload weapons), so this is a rendering slice, not a systems one.
  Smallest useful first cut: root = solid-tint sprite, payload = outline/reduced-opacity sprite —
  one channel, purely derived from data that already exists, matching the doc's "solid fill vs.
  outline alone is sufficient" note.
- **Color channel is spent twice on paper.** The Handoff and the (mostly superseded)
  `glyph_design_grammar.md` both reserve color for *damage type*. The shipped tooltip glyphs
  (`ChainComponentColors`, `TypeGlyphs`) already spend color on *component role* instead, and no
  damage-type enum exists in code yet, so there's no live conflict today — but if elemental damage
  types ever land, whoever adds them will need to pick a *different* channel (or accept overloading
  one channel with two meanings). Worth a real design-gate conversation before that day arrives,
  not a silent choice either way.
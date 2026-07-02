
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
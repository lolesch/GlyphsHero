
Two levels of interaction:
- **Unit level** — effects triggered by allies/enemies within effect shapes on the hex map.
- **Item level** — [[Weapon#Root Mode|Weapon]] attacks that drive moment-to-moment combat.

These two levels synergize but do not conflict. Unit abilities handle inter-unit effects. Item chains handle what the unit _does_ when it attacks.

---

# Attack Combo / Synergies
Parallel Execution
multiple Weapons fire simultaneously
- how to create combos (besides payload)
	- effect stacking and conditions -> pawn/hex layer, not inventory!
- timed interactions - apply water, then apply lightning and so on

---

# Hex-grid Properties
- position / positioning
- terrain / obstacles / LoS
- attack shape / range 
- movement / projectiles

**Positional conditions** (flanked, adjacent to ally, terrain type) — deferred to hex layer. See [[Pawn|Pawn Design]].

---

# Combat Events

> [!info] Implemented today
> `ReactorType` has 6 live values: `OnSelfHit`, `OnManaDeplete`, `OnEnemyDeath`, `OnAllyAttacks`,
> `OnAllyKills`, `OnNearbyEnemyDies`. Everything else below (the wider event vocabulary, hex-positional
> conditions) is forward-looking design — the `OnAllyAttacks`/`OnAllyKills`/`OnNearbyEnemyDies` events
> in particular still need `CombatCoordinator` cross-pawn access per KNOWN_ISSUES.

- OnDamageTaken
- OnManaSpent
- OnOverheal

support hex positioning for "nearby" and similar

- OnNearbyAllyGotHit
- OnOutOfVision
- OnFlanked/Surrounded

careful with [[Reactor]] 

- OnEnemyHit

# Event List

**Self — receiving damage**

|Event|Notes|
|---|---|
|This unit is hit (any)||
|This unit is critically hit||
|This unit is hit for X% max HP in one strike||
|This unit takes damage of a specific type||
|This unit is stunned||
|This unit is debuffed||
|This unit is flanked / surrounded|_Deferred — hex layer_|

**Self — attacking**

|Event|Notes|
|---|---|
|This unit attacks||
|This unit hits||
|This unit misses|Underexplored — risk/reward potential|
|This unit crits||
|This unit kills||
|This unit overkills||
|This unit stuns an enemy||

**Ally events (hex radius)**

|Event|Notes|
|---|---|
|Any ally attacks||
|Any ally hits||
|Any ally crits||
|Any ally kills||
|Any ally takes damage||
|Any ally dies||
|Ally count drops below X||
|Ally enters a special state (rage, etc.)||

**Enemy events (hex radius)**

|Event|Notes|
|---|---|
|Nearby enemy dies||
|Nearby enemy is debuffed / poisoned||
|Nearby enemy is stunned||
|Enemy enters adjacent hex|_Deferred — hex layer_|
|Enemy count drops below X||

**State / threshold events**

|Event|Notes|
|---|---|
|This unit's HP drops below X%||
|A buff is consumed|From Backpack Battles|
|Chain propagates through this weapon (payload fired)|Unique to this system|
|This unit is last surviving ally||

---

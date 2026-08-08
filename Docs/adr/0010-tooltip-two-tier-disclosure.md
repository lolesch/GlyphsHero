---
tags:
  - ADR
  - UI
status: Accepted
date: 2026-08-07
---

# ADR-0010 — Tooltip is a two-tier disclosure: Tier-1 shows only the active context, Details mode is the one "why" layer

**Status:** Accepted (2026-08-07)
**Lifecycle:** Implemented (2026-08-08) — Decisions 1/2/4 landed in `ItemTooltipController.cs`
(`AppendAttachmentIdentity`, the chained-weapon/standalone-weapon `.Other` lines, `AppendPayloadOutput`),
red-green-tested in `ItemTooltipControllerTests.cs`; Decision 3 landed earlier (commit `f7e4a5d`).
**Companion:** ADR-0004 (attack model — item roles: weapon/payload/attachment), ADR-0006 (payload
propagation cost economy), ADR-0009 (generic Cost `inputMod` — accepted, not yet implemented; this
ADR's tooltip rules apply to it once it lands, no changes needed here when it does).
**Refines:** the 2026-06-30 tooltip-redesign spec's locked rule
(`Docs/superpowers/specs/2026-06-30-tooltip-redesign.md`, §2 "Both states always visible; Alt expands
the math") — specifically the "both states always shown, other dim" rule. That spec logged its
decisions as two-way-door UI ledger entries, not an ADR; this one supersedes exactly that rule (plus
the always-visible payload-mechanics behaviour it shares a rationale with) because human review
(2026-07-02, tracked as [#26](https://github.com/lolesch/GlyphsHero/issues/26)) found the locked
convention itself was the problem, not any one slice's execution. The v2 addendum's other rules
(header layout, two-state fixed order, stash unchained value, Shifter's visual family, universal stat
glyphs) are **not** touched by this ADR — they're independent bugs/slices tracked on their own issues
(#18, #20, #21, #23, #24) and remain governed by the spec as written.
**Context:** Tooltip v2 (issues #17–#25) shipped per-component against that spec, but the 2026-07-02
human verification pass found the components didn't add up to one legible system — each did its own
thing. That opened #26 (`needs-design`, blocking) rather than patching each slice. Leonid iterated on
a replacement layout in Figma (`GlyphsHero UI`, node `13-2`) and walked it through with worked
examples; this ADR is that design-table decision, written down before more slices get built against
the old convention.

---

## Context

The current tooltip (`ItemTooltipController.cs`, `TwoStateBlock.cs`, `PositionalDelta.cs`) locked two
rules in the 2026-06-30 spec that the Figma review now reverses:

1. **"Both states always shown, other dim"** (`TwoStateBlock.cs:53-59`, consumed unconditionally at
   `ItemTooltipController.cs:264-265, 493-494, 516`). Every hover shows the active state bold *and*
   the counterfactual state dim beneath it, in both default and Details mode.
2. **Per-type Alt content is inconsistent about *how* a delta expands.** The weapon-terminal path
   (`PositionalDelta.BaseFinal`, `ItemTooltipController.Stat`) already renders `base → result` under
   Details. But `PositionalDelta.Describe` — the content for an attachment's *own* hover
   (`ModLine`/`AffixLines`, `PositionalDelta.cs:168-194, 220-222`) — takes no `detailed` parameter at
   all and always prints the raw `Modifier.ToString()` (e.g. `PercentAdd +50` → `"+50 %"`). This is
   the exact bug in [#29](https://github.com/lolesch/GlyphsHero/issues/29): hovering an Amplifier in
   Details mode shows `+50 % Damage` (raw modifier) while the weapon's own piece-list row for the same
   Amplifier correctly shows `10 → 15` (via `Stat()`), because the piece list and the attachment's own
   `Describe()` are two different code paths that never agreed to render the same way.

Leonid's Figma pass (`Docs/GlyphsHeroDesign/New Design.pdf`, `Section 1.pdf`) proposes fixing both by
splitting the tooltip into two disclosure tiers instead of patching each symptom separately, with his
own annotation flagging the state-visibility change specifically: *"I'd demote the current
always-both TwoStateBlock here — the inactive state is planning info, not combat info."*

## Decisions

### 1. Tier-1 (default, no Details) content is a fixed whitelist: name+kind, the active-context effect only, the attack sentence, the trigger, and one payload flag line — Accepted

Default-mode tooltip content is exactly:

1. **Name + kind** — unchanged (glyph/color already encode kind).
2. **The active effect in its current context, and only that.** Weapon-chained shows effective
   Damage/AttackSpeed/Cost+pool (unchanged — `AppendWeaponTerminal`/`Stat()` already do this).
   Attachment-chained shows its contribution delta (unchanged — `PositionalDelta.Describe`'s compact
   form). Anything unchained shows its passive affix (unchanged — `AffixLines`). What's **removed**:
   the counterfactual/other state no longer renders in Tier-1 at all (Decision 2).
3. **Attack-type sentence** — unchanged (`DeliverySentence.Build`).
4. **Trigger** — unchanged (`PositionalDelta.FiringCondition`, already surfaced on the reactor-driven
   rate line and the piece list's Reactor row).
5. **Payload flag, one compact line, only if present** — unchanged (`AppendPayloadSummary`'s
   `"N payloads: cost"` line on the driving weapon's own tooltip).

*Why:* items 2–5 are already what the shipped code renders in Tier-1 today — this decision's only
actual change is removing the counterfactual state from the list (Decision 2). Writing the whole
whitelist down here is what makes "the active effect... and only that" enforceable: it's the rule
[#26](https://github.com/lolesch/GlyphsHero/issues/26) asked for (one legible system), not a
per-component judgment call.

### 2. The counterfactual/other state moves from "always dim-visible" to "Details-only" — presenter-level gate, no change to `TwoStateBlock`'s data shape — Accepted

`TwoStateBlock.Build` keeps returning `(Active, Other)` exactly as it does today — the pure model is
unchanged. What changes is the **presenter**: every call site that currently appends `.Other`
unconditionally gates it behind `detailed`:

- `ItemTooltipController.cs:264-265` (a chained weapon's dim "as payload"/"as driving weapon" line)
- `ItemTooltipController.cs:493-494` (`AppendAttachmentIdentity`'s dim unchained/chained line)
- `ItemTooltipController.cs:516` (`AppendStandaloneWeapon`'s dim "as payload" line)

Each becomes `if (detailed) AppendState(sb, ..., Other, emphasized: false);`. `.Active` keeps
rendering unconditionally in both modes — Tier-1 never loses the live read, only the counterfactual.

*Why:* Leonid's framing — the inactive state is planning info ("what would this look like the other
way"), not combat info ("what does this do right now"). Keeping it in the always-visible tier was the
[#26](https://github.com/lolesch/GlyphsHero/issues/26)-triggering symptom: it doubled every hover's
line count regardless of whether the player asked for the comparison. Gating in the presenter rather
than removing it from `TwoStateBlock` keeps the change mechanical (one `if` per call site) and keeps
`TwoStateBlockTests` valid without a rewrite — the model still produces both states, only the tooltip
controller decides when `Other` is worth printing.

### 3. Every Details-mode stat delta resolves to `base → result`, regardless of `ModifierType` — `PositionalDelta.Describe` gains the same expansion `Stat()`/`BaseFinal()` already give the weapon-terminal and piece-list paths — Accepted

`PositionalDelta.Describe(item)` gains a `detailed` parameter (mirroring `AxisDeltas`, `BaseFinal`,
`ReactorInputEquation`, which already take one). When `detailed` is true, `ModLine` and the Shifter/
Reactor/Converter branches resolve the *same* positional-delta diff the piece list already computes
for this item (`WeaponStatResolver.Resolve(weapon, ordered.Take(i))` vs `…Take(i+1)`, i.e. reusing
`PositionalDelta.Pieces`' math for the hovered item's own row) instead of printing
`Modifier.ToString()` directly. `AffixLines` gets the equivalent treatment for the *unchained* side
once [#22](https://github.com/lolesch/GlyphsHero/issues/22)'s owned-vs-stash distinction is wired
(this ADR doesn't reopen #22's scope, it just requires whatever unchained expansion #22 builds to use
this same `base → result` shape rather than a third format).

*Why:* this is exactly the fix for [#29](https://github.com/lolesch/GlyphsHero/issues/29) — the bug
isn't that Damage's formatting is wrong in isolation, it's that `Describe()` and the piece list
disagree about what Details mode means for the same underlying number. There is already one correct
implementation of "expand a delta to base → result" in this file (`Stat()`, `BaseFinal()`,
`ReactorInputEquation`); `Describe()` reusing it (rather than growing a fourth formatter) is what makes
"all stat changes rendered the same way" (the Figma spec's own wording) actually true instead of
aspirational.

### 4. Payload mechanics (marginal cost type, timing, shape size) move from always-visible to Details-only on the payload's own hover; Tier-1 payload read stays damage + delivery sentence — Accepted

`AppendPayloadOutput` (`ItemTooltipController.cs:440-466`) currently prints its cost line and timing
line unconditionally. Per Decision 1's whitelist, only damage + delivery sentence (item 2) stay in
Tier-1 for a hovered payload weapon; the cost-value/cost-type line (`CostNote`) and the timing line
move behind `detailed`. The compact one-line payload flag on the *driving* weapon's own tooltip
(`AppendPayloadSummary`, Decision 1 item 5) is unaffected — that line already is the Tier-1 payload
read for someone hovering the weapon, not the payload.

*Why:* cost-scaling type (`flat` / `% of base` / `deeper-costs-more` / `fixed`) and delayed-timing
values are optimizer detail — useful for deciding whether to stack a payload deep, irrelevant to "what
does this do right now." Matches the Figma spec's own classification of payload mechanics as a
Details-layer bullet.

## Worked example

Chain: **Reactor "Spark Trigger"** (root; `ReactorType.OnSelfHit`; `inputMod` = `ManaCost` `PercentAdd`
`+20`) → **Amplifier "Ember Amp"** (`outputMod` = `Damage` `PercentAdd` `+50`) → **Weapon "Iron Blade"**
(base Damage 10, base AttackSpeed 2.0/s, base ResourceCost 4 Mana) carrying a payload, **"Spark
Bolt"** (own Damage 3, Aoe r1/Hostile/Target, cost 2 Mana `FlatAdd`, `PayloadTiming.Delayed` 0.3s).

**Resolving Iron Blade** (`WeaponStatResolver`, contributors = Reactor + Ember Amp, unaffected by this
ADR): Damage — `FlatAdd` bucket empty, `PercentAdd` bucket = Ember Amp's +50% → `10 × 1.5 = 15`. Cost —
`PercentAdd` bucket = Reactor's +20% → `4 × 1.2 = 4.8` Mana. Reactor-driven, so the rate line shows the
firing condition instead of an interval.

**Hovering Iron Blade, Tier-1 (default):**
```
⚔ Iron Blade                                    [Weapon]
────────────────────────
Attack: (reactor-driven)
  dmg  15                fires when hit
  Strikes a single enemy at the target
  cost 4.8 [Mana]  (root gate)
  ▸ Spark Trigger  fires when hit   ManaCost +20 %
  ◆ Ember Amp      15
  1 payload: +2 [Mana]
```
No `base →` prefixes, no "as payload" counterfactual line beneath — Decision 1's whitelist end to end.

**Hovering Iron Blade, Details mode** — every delta above gains its `base → result` (unchanged
behaviour: `AppendWeaponTerminal`/`Stat()`/`BaseFinal` already do this) *and* the dim "as payload" line
now appears (Decision 2, newly gated in rather than always-on):
```
  dmg  base 10.0 → final 15.0     fires when hit
  cost base 4.0 → final 4.8 [Mana]   (root gate)
  ▸ Spark Trigger  fires when hit   [base 4] +20 % = 4.8
  ◆ Ember Amp      10.0 → 15.0
  1 payload: +2 [Mana]
  as payload:   3.0 dmg   Blasts enemies within 1 of the target
```

**Hovering Ember Amp directly, Tier-1:** `Damage +50 %` (unchanged — Decision 1 item 2's compact
active-context read; no counterfactual line beneath, per Decision 2).

**Hovering Ember Amp directly, Details mode — this is the #29 fix:** Decision 3 makes `Describe`
resolve the same positional diff the piece list already computed (before = chain up to but excluding
Ember Amp → Damage 10; with = including it → Damage 15), so the line reads `10.0 → 15.0` — matching
the piece-list row exactly, instead of today's `+50 %` regardless of Details. The unchained affix line
now also appears beneath it (Decision 2, gated in), dim.

**Hovering Spark Bolt (the payload), Tier-1:** `3.0 dmg` + `Blasts enemies within 1 of the target` —
Decision 4's damage + delivery sentence, nothing else. **Details mode** adds the cost line
(`cost +2 [Mana]  ·  flat`) and the timing line (`delayed (0.3)`), previously always-on, now
Details-only.

Every Decision above is exercised by this one chain: Decision 1 (Tier-1 whitelist on both the weapon
and the attachment), Decision 2 (the "as payload"/unchained-affix lines appearing only under Details),
Decision 3 (Ember Amp's Details-mode line agreeing with the piece list), Decision 4 (Spark Bolt's cost/
timing demoted). No step required a judgement the Decisions don't answer.

## Considered and rejected

- **Remove the counterfactual state from `TwoStateBlock` entirely, computing it only on demand.**
  Rejected — Details mode still needs it, and `TwoStateBlock`'s existing `(Active, Other)` shape
  already supports "compute both, let the presenter decide what to show." Changing the model would
  touch `TwoStateBlockTests` for no behavioural gain over a presenter-level `if`.
- **Give `Describe()` its own `base → result` formatter instead of reusing `Stat()`/`BaseFinal()`'s.**
  Rejected — that's exactly how the tooltip ended up with two disagreeing formats in the first place
  ([#29](https://github.com/lolesch/GlyphsHero/issues/29)); one shape, reused, is the point.
- **Keep payload cost/timing always-visible and only demote the counterfactual state.** Rejected —
  the Figma review classified payload mechanics as Details-layer content on the same reasoning as the
  counterfactual state (optimizer/planning info, not the instant combat read); doing one but not the
  other would leave Tier-1 inconsistent between item families, the thing #26 opened to fix.

## Deferred (designed, not built)

- **Derived numbers** (DPS, cost-per-second, damage-per-mana) — the Figma spec names this as a
  Details-mode content class, but no formula, threshold, or exact wording was decided in this pass.
  Not rendered anywhere today. File as a new issue once the exact numbers/format are picked; out of
  scope for this ADR's Decisions (none of the Worked example's steps depend on it existing).
- **Modifier mechanics tags** (`FlatAdd`/`PercentAdd`/`PercentMult`/`Overwrite` labels on a delta, for
  optimizers) — same status: named as a Details-layer content class, no concrete rendering decided.
  `CostNote`'s existing `flat`/`% of base`/`deeper-costs-more`/`fixed` payload-cost labels are the one
  place this already exists in some form (now Details-gated per Decision 4); generalizing it to every
  stat delta is future scope, not this ADR.
- **Glyph rendering mechanism** ([#27](https://github.com/lolesch/GlyphsHero/issues/27) — TMP
  sprite-asset switch) and **header icon prefab wiring** ([#28](https://github.com/lolesch/GlyphsHero/issues/28))
  are unaffected by this ADR — both issues are explicit that they're independent of the layout
  decision (#27: "governs *which* slots the glyphs sit in; this one is purely *how* a glyph renders").

## Open questions

None blocking acceptance — the Worked example traces every Decision without an unanswered judgement
call. (#22's owned-vs-stash unchained-expansion scope is referenced in Decision 3 but not reopened; it
resolves on its own schedule and just needs to land in the `base → result` shape once it does.)

## Consequences

- **Positive:** resolves [#26](https://github.com/lolesch/GlyphsHero/issues/26)'s actual ask — one
  governing rule for what Tier-1 shows versus what's Details-only, replacing per-component judgment
  calls — and is the accepted fix design for [#29](https://github.com/lolesch/GlyphsHero/issues/29)
  (Decision 3). Every change is a presenter-level gate or a formatter reuse — no
  `TwoStateBlock`/`PositionalDelta` data-shape changes (beyond the `Describe` signature, see below), no
  `ChainResolver`/`WeaponStatResolver` changes, no new domain concepts.
  **Scope check — this ADR does *not* resolve:** [#18](https://github.com/lolesch/GlyphsHero/issues/18)
  (Shifter's piece-list visual family — a grouping/glyph bug, orthogonal to disclosure tier),
  [#19](https://github.com/lolesch/GlyphsHero/issues/19) (weapon-terminal totals still render every
  stat unconditionally — Decision 1 describes the *intended* additive behaviour but implementing it is
  untouched by any Decision here), [#20](https://github.com/lolesch/GlyphsHero/issues/20) (two-state
  fixed order + icon labels — `TwoStateBlock.Build` still swaps positional order by `primaryActive`;
  compatible with Decision 2's Details-gate but a separate fix), or
  [#23](https://github.com/lolesch/GlyphsHero/issues/23)/[#24](https://github.com/lolesch/GlyphsHero/issues/24)
  (cost-line icon+multiplier styling, universal stat glyphs — presentation detail this ADR doesn't
  decide). Those stay open as independent slices, now with ADR-0010 as governing context so their
  implementations don't contradict the Tier-1 whitelist or the Details-only gate.
- **Negative / debts:** every call site listed in Decision 2 needs its `detailed` gate added and a
  test proving the gate (red-green: assert `.Other`/payload-cost/timing lines are absent in Tier-1,
  present in Details). `Describe()`'s signature change (Decision 3) ripples to every caller
  (`TwoStateBlock.Build`'s two `PositionalDelta.Describe(item)` call sites) and needs the chain/context
  threaded through that `TwoStateBlock` doesn't currently carry — `TwoStateBlock.Build` takes only the
  item today, not its chain, so it will need a `chain` (or `IItemChain`) parameter to compute the
  positional diff Decision 3 requires. That's a real signature change to `TwoStateBlock.Build`, not
  just `Describe`, and should be scoped explicitly in whichever issue implements Decision 3.

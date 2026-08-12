---
tags:
  - ADR
  - UI
status: Accepted
date: 2026-08-13
---

# ADR-0011 — The weapon-terminal cost line drops the "(root gate)" annotation

**Status:** Accepted (2026-08-13)
**Lifecycle:** Implemented (2026-08-13) — `ItemTooltipController.cs` (`AppendWeaponTerminal`,
`AppendChainOutput`)
**Amends:** [[0010-tooltip-two-tier-disclosure|ADR-0010]] §Worked example — the two rendered cost-line
blocks (`cost 4.8 [Mana]  (root gate)` / `cost base 4.0 → final 4.8 [Mana]   (root gate)`) no longer
match the code; see `## Worked example` below for the corrected text. No other line in that worked
example is affected, and ADR-0010's Decisions themselves are untouched.
**Companion:** ADR-0006 (payload propagation cost economy — the source of "root gate" as a term of art),
ADR-0010 (tooltip two-tier disclosure — the worked example this amends).
**Context:** During the tooltip-redesign v2 slice 8 stat-label work (routing player-facing stat names
through `StatGlyphs.Label` instead of raw enum text), the `"   (root gate)"` suffix on the weapon's
resolved-cost line was dropped from both render sites as part of the same pass, on the grounds that
"root gate" is `ChainResolver`/ADR-0006 vocabulary, not copy a player has any reason to know. That
change shipped without amending ADR-0010, whose Worked example locks in the annotation as part of the
"this is what it looks like" record — a code review surfaced the resulting mismatch between the
Accepted/Implemented ADR and the actual rendered output. This ADR is the retroactive design-table
sign-off on that call: keep the removal, and correct the record instead of reverting the code.

---

## Context

ADR-0010 (Accepted, Implemented 2026-08-08) fixed the tooltip's two-tier disclosure shape and included a
full worked example of "Iron Blade," a reactor-driven weapon with a resolved cost of 4.8 Mana. Both the
Tier-1 and Details renderings of that example show the cost line suffixed with a dim `(root gate)` tag —
at the time, a call-out that this particular number is special: per ADR-0006, it's the fail-forward gate
combat checks before the weapon fires at all, not just another resolved stat.

Separately, and later, the v2 slice 8 stat-label work (making `StatGlyphs.Format`'s Details-mode label
route every stat through a small player-facing name map instead of raw PascalCase enum text) touched the
same cost line and dropped the `(root gate)` suffix as part of that pass. The reasoning captured in the
code comment at the time: "root gate" names an internal mechanism (`ChainResolver`/ADR-0006), and stacking
a second, jargon-laden annotation onto a line that was simultaneously gaining its own `StatGlyphs.Format`
label (`$ 4.8 Cost`, under Details mode) read as cluttered and unexplained to a player who has never heard
the term. No superseding ADR accompanied that change, so ADR-0010's worked example silently stopped
matching the rendered output.

## Decisions

### 1. The weapon-terminal cost line renders as a plain resolved value, with no "(root gate)" tag — Accepted

`AppendWeaponTerminal` (Tier-1) and `AppendChainOutput` (Details/chain-output path) render the weapon's
resolved cost the same way as every other terminal stat — through `StatGlyphs.Format`/`Stat()` — with no
trailing annotation calling out that this particular value is also a fire/fizzle gate. *Why:* "root gate"
is developer and ADR vocabulary describing a *mechanism* (ADR-0006's fail-forward check), not a concept
the tooltip needs to name for a player to use correctly — the cost number itself, next to the resource
pool it draws from (`[Mana]`), is enough for a player to judge affordability without being told the
mechanism has a name. Keeping the line free of jargon is consistent with the same slice's broader move
(`StatGlyphs.Label`) away from printing internal identifiers as player-facing copy.

## Worked example

Same scenario as ADR-0010's own worked example (Iron Blade: Reactor "Spark Trigger" → Amplifier "Ember
Amp" → Weapon "Iron Blade", resolved cost 4.8 Mana). The two cost lines it showed are the only lines that
change; everything else in that worked example still renders exactly as ADR-0010 recorded it.

**Tier-1** — was `cost 4.8 [Mana]  (root gate)`, now:
```
  cost 4.8 [Mana]
```

**Details mode** — was `cost base 4.0 → final 4.8 [Mana]   (root gate)`, now:
```
  cost base 4.0 → final 4.8 [Mana]
```

## Deferred (designed, not built)

- ADR-0006 already tracks "Telegraphing fired-vs-fizzled nodes" as an open legibility debt (its own
  `## Consequences`). This ADR removes the one in-UI hint that pointed at that debt (the `(root gate)`
  tag) without adding a replacement — if fired-vs-fizzled telegraphing is ever built, it should be
  designed as its own visual treatment (e.g. a color/state cue tied to whether the pool can currently
  afford the cost), not by reinstating this text tag.

## Consequences

- **Positive:** the cost line reads as plain UI copy consistent with the rest of the v2 slice 8 label
  work; no internal-mechanism vocabulary leaks into player-facing text. ADR-0010's worked example is
  corrected to match shipped behavior instead of silently drifting from it.
- **Negative / debts:** the tooltip no longer visually distinguishes "this cost is a hard fire/fizzle
  gate" from an ordinary resolved stat value — the fail-forward mechanism (ADR-0006) is now entirely
  implicit. This sharpens, rather than resolves, ADR-0006's existing telegraphing debt (see Deferred,
  above).

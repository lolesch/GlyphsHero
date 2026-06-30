# Architecture Decision Records

Each ADR records **one decision and its rationale, frozen at the moment it was made.** ADRs are how
this project stops design intent from drifting away from the code — but only if everyone reads and
writes them the same way. That convention lives here.

> New ADR? Copy [`0000-template.md`](0000-template.md), don't hand-roll the skeleton. The header
> fields below are what keep ADRs drift-proof; a hand-rolled one usually drops one.

## The one rule: the body is frozen, the header is live

An ADR body is a **point-in-time record.** Once an ADR is `Accepted`, **don't edit its Decisions to
match what the code does now** — that turns the ADR back into a live document you have to maintain, and
re-introduces exactly the drift ADRs exist to prevent. When reality moves on:

- **What changed** goes in the **header** of *this* ADR (`Lifecycle`, `Amended-by`) and in the **body
  of the *new* ADR** that supersedes it.
- The **current truth** of a concept lives in `CONTEXT.md` (the glossary), which points at the
  governing ADR. So: glossary = "what is true now," ADR body = "what we decided then, and why."

**Reading an ADR:** trust the **header** for live state. A frozen Decision in the body may have been
withdrawn or amended — the header's `Amended-by` line is the authority, not the Decision prose.

## Header schema

Directly under the `# ADR-NNNN — title` line, as bold key/value lines:

| Field | Meaning | Always present? |
|---|---|---|
| **Status** | the *decision's* standing: `Proposed` / `Accepted` / `Superseded by ADR-NNNN`. Mirrors the YAML `status:`. | yes |
| **Lifecycle** | the *code's* standing, the most-read live field: `Design-only — not implemented` / `Implemented YYYY-MM-DD` / `Partially implemented (…)`. Update this field when the code lands; **do not** narrate the build inside the body. | yes |
| **Amends** | earlier ADRs this one changes, with the section + verb: `ADR-0001 §2b (withdrawn), §3 (amended)`. | if it amends |
| **Amended-by** | back-pointer, filled in on the *older* ADR when a newer one amends it: `ADR-0004 §2 (Reach unified)`. Keeps the old ADR honest without editing its Decisions. | when amended |
| **Companion** | related ADRs that don't amend each other, read-together context. | optional |
| **Refines** | a softer `Amends` — fills in a deferred axis of an earlier ADR without contradicting it. | optional |
| **Context** | one-paragraph "why now" lead-in (or a `## Context` section for longer ones). | yes |

The YAML frontmatter stays: `tags: [ADR, <domains>]`, `status:`, `date:`.

## The acceptance gate: prove it before Accepted

A **structural** ADR (one that defines rules) stays `Proposed` until its `## Worked example` section
traces one concrete case end-to-end through every Decision. That worked example is what catches a
contradiction *between* Decisions before code depends on it — the failure mode that produced ADR-0006's
back-written "Deviation from Decision 5" note. If the example can't be walked without a judgement the
Decisions don't answer, that's an `## Open questions` entry to resolve before acceptance. Pure
tuning/number ADRs are exempt — their validation is playtest. Full rationale: `Docs/agents/design-gate.md`.

## Body sections (conventional order)

`## Context` → `## Decisions` (numbered `### N. … — Accepted` subheads, each with its *Why*) →
`## Worked example` (required for structural ADRs — the acceptance gate above) →
`## Considered and rejected` → `## Deferred (designed, not built)` → `## Open questions` →
`## Consequences`.

Keep `## Consequences` **decision-level** (what this commits us to, the debts incurred). The blow-by-blow
of *how it was built* — file lists, "Phase 1/2 … Done," test counts — belongs to the **git commits and
issue trail**, not the ADR. The `Lifecycle` header field is the ADR's pointer at that build state.

## Index

| ADR | Title | Lifecycle |
|---|---|---|
| [0001](0001-range-movement-and-combat-tick.md) | Range as a pawn stat, monotone-closing movement, fixed combat tick | Implemented (amended by 0004 §2) |
| [0002](0002-hex-occupancy-damage-and-telegraph-contract.md) | Hex grid is the damage contract; occupancy + telegraph | Implemented |
| [0003](0003-delivery-patterns-reach-gated-and-stackable.md) | Delivery patterns are reach-gated, stackable, covered-hex | Implemented (refined by 0004) |
| [0004](0004-attack-model-item-roles-and-recursive-delivery.md) | Attack model: item roles + recursive-delivery collapse | Implemented |
| [0005](0005-resource-economy-cost-gain-magnitude.md) | Resource economy decomposes into Cost / Gain-on-hit / Magnitude | Implemented |
| [0006](0006-payload-propagation-cost-economy.md) | Payload propagation is a fail-forward cost economy | Implemented |
| [0007](0007-weapon-payload-direction-and-reactor-boundary.md) | Weapon→weapon payloads; reactor is the firing boundary; age-stamped origin | **Partially implemented** (Decision 1 only) |

Keep this table's `Lifecycle` column in step with each ADR's header when state changes.

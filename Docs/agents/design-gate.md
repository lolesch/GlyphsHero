# Design gate — deciding before building

The failure this prevents: a design fork getting resolved **at the keyboard, mid-implementation, and
back-documented as if it were always the plan.** (Lived example: ADR-0006 shipped with a "Deviation
from Decision 5" paragraph grafted onto its header — Decision 5's literal wording leaked fork-sibling
cost, contradicting Decision 3, and that conflict was settled while writing the walker, not while
deciding the design.) The design is the source of truth; when implementation outpaces it, we **pause
and decide**, we don't drift.

This is not a ban on judgement — a rule that forbids every assumption forbids all progress, and this is
a playtest-driven game where some things are only knowable by building and playing. The gate is about
*which* gaps stop work, and *where* the decision gets recorded.

## The threshold: one-way vs two-way doors

Not every undefined edge is a design fork. Apply this test before filling a gap:

> **Would undoing this after three more slices be cheap?**

- **Two-way door** (cheap to reverse — a tuning value, an ordering tiebreak, a default): **decide it, but
  log it.** It lands in the slice-end ledger (below) for veto. Don't stop the work for it.
- **One-way door** (expensive to unwind once code depends on it; *contradicts* an accepted Decision; or
  *defines a previously-undefined rule* rather than a value): **stop.** Surface it as a `needs-design`
  item; do not settle it silently and do not back-write the choice into an accepted ADR.

The ADR-0006 sibling-leak was a one-way door (it changed what the cost model *means*). The epsilon value
guarding blood-magic self-kill was a two-way door.

## Acceptance gate: prove the design before building it

A **structural** design (one that defines rules — an ADR) is not `Accepted` until **one worked example
traces a concrete case end-to-end through every Decision.** Grill the design first (the `grilling`
skill / a `/grill`-style pass), then write the worked example. If the example can't be walked without a
judgement call, that call is an open decision — surfaced *now*, as a `## Open questions` entry or a
`needs-design` issue, not discovered later in code.

- This is enforced by the ADR template: structural ADRs carry a required **`## Worked example`** section
  (see `Docs/adr/0000-template.md`). No worked example → the ADR stays `Proposed`.
- **Tuning is exempt.** Numbers, balance, and feel are validated by *playtest*, not by approval. Don't
  formalise a decision whose real test is how the game plays — over-freezing tuning is its own drift.
- This is a *pre*-acceptance step, so it doesn't conflict with the append-only rule: the worked example
  is part of the body at decision time and frozen thereafter, like every Decision.

Grilling kills *known* unknowns. It is blind to facts implementation hands you (a contradiction you can
only see once you build the walker; a Decision that turns out redundant once wired). Those are caught by
the next two mechanisms, not by the gate — a second drift-catch at implementation is expected, not a
failure of the gate.

## `needs-design`: where open forks live

A one-way-door gap becomes a `needs-design` GitHub issue (see `triage-labels.md`).

- **Mutually exclusive with `ready-for-agent`** — an issue is never both. This is what keeps the night
  shift unblocked: the runner's filter is a *whitelist* (`ready-for-agent`), so it never even sees a
  `needs-design` issue. `needs-design` is a queue for the **day shift** (the human + interactive agent),
  not a blocker on the runner.
- **"Resolved" has a definition:** the decision is recorded in `CONTEXT.md` (and a new / superseding ADR
  if it changes a frozen one) — *then* the label clears and the issue may become `ready-for-agent`. The
  label is the entry point; the recorded decision is the resolution. A cleared label with no recorded
  decision is drift wearing a green checkmark.

## Slice-end ledger: make the silent fill impossible to miss

Every implementation slice ends with a fixed block — **unconditionally**, whether or not a fork came up.
This is the part that catches the *silent* fill, the one with no phrase to spot, because it doesn't
depend on either of us noticing in the moment:

```
### Slice ledger
- Assumptions made:      <two-way doors I decided — review/veto these>
- Decisions I took:      <anything that leaned on judgement, with the door test result>
- Gaps left open:        <one-way doors I did NOT fill — each a needs-design candidate>
```

- **Day shift:** emit it in chat at the end of the slice.
- **Night shift:** emit it into the per-issue night-runner comment and the handover. A "Gaps left open"
  entry means a `needs-design` issue was filed (see below).

You veto from a list, not by reading prose for tells.

## Night shift: park, don't guess; move on, don't halt

When the runner hits a one-way-door fork *mid-slice* (the ADR-0006 situation), it must neither silently
fill it (drift) nor stall the whole night (blocked):

1. Commit only the already-decided **safe** part of the work.
2. Open a `needs-design` issue capturing the fork (what's undefined, the options seen, why it's one-way).
3. Strip `ready-for-agent` from the original issue.
4. Pick up the **next** eligible `ready-for-agent` issue.

"Don't block the night" never means "guess to keep moving" — it means "skip to the next funded issue,"
the same shape as the propagation-cost walker pruning an unaffordable node and continuing. The runner's
instruction for this lives in `.claude/work-prompt.md` on `night-base`.

## The minimum

Drop any one and a specific hole reopens:

- no **acceptance gate** → contradictions ship inside accepted ADRs;
- no **`needs-design`** → gaps have nowhere to live and get filled instead;
- no **slice ledger** → silent fills stay silent.

That's the floor, not a menu.

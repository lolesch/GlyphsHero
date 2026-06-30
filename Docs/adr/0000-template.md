---
tags:
  - ADR
  - <domain>
status: Proposed
date: YYYY-MM-DD
---

# ADR-NNNN — <one-line decision, stated as a claim>

**Status:** Proposed (YYYY-MM-DD)
**Lifecycle:** Design-only — not implemented
**Companion:** <related ADRs, if any>
**Refines / Amends:** <earlier ADR + §section + verb, if any — e.g. ADR-0001 §3 (amended)>
**Context:** <one paragraph: why this is being decided now, what's broken or unresolved without it>

<!--
  Convention reminder (delete before committing): see README.md.
  - The body is FROZEN once Accepted. Don't edit Decisions to match later code.
  - ACCEPTANCE GATE (Docs/agents/design-gate.md): a structural ADR stays Proposed until the
    `## Worked example` below traces one concrete case through every Decision. If you can't walk
    it without a judgement call, that call is an `## Open questions` entry — decide it before Accepted.
  - When this lands in code, update the Lifecycle header to "Implemented YYYY-MM-DD".
    Do NOT narrate the build (file lists / "Phase 1 … Done" / test counts) in the body —
    that's the commit + issue trail. Keep Consequences decision-level.
  - When a later ADR changes this one, add an **Amended-by:** line here; don't rewrite Decisions.
-->

---

## Context

<the forces in tension; the mismodel this corrects, if any>

## Decisions

### 1. <decision stated as a claim> — Accepted

<what is decided.> *Why:* <the rationale — this is the load-bearing part.>

## Worked example

<REQUIRED for a structural ADR (acceptance gate — Docs/agents/design-gate.md). Trace ONE concrete
case end-to-end through every Decision above, with real values. This is what proves the Decisions
don't contradict each other before any code depends on them. If walking it forces a judgement the
Decisions don't answer, that's an `## Open questions` entry — resolve it before this ADR is Accepted.
Omit only for a pure tuning/number ADR, whose real validation is playtest.>

## Considered and rejected

- **<alternative>** — <why not.>

## Deferred (designed, not built)

- <scoped follow-ups intentionally out of this decision.>

## Open questions

- <what this ADR deliberately leaves unresolved, and where it'll be decided.>

## Consequences

- **Positive:** <what this buys.>
- **Negative / debts:** <what it costs; telegraphing/owed work.>

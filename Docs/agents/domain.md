# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`CLAUDE.md`** at the repo root — the current high-level architecture and domain reference (assembly layering, core runtime flows, the chain system, conventions). Read this first.
- **`CONTEXT.md`** at the repo root, if it exists — the glossary / ubiquitous language.
- **`Docs/adr/`**, if it exists — read ADRs that touch the area you're about to work in. Read
  **`Docs/adr/README.md`** first: an ADR **body is frozen at decision time**, so trust the **header**
  (`Lifecycle`, `Amended-by`) for live state — a frozen Decision may have been withdrawn or amended by a
  later ADR. The *current* truth of a concept lives in `CONTEXT.md`, which points at the governing ADR.

If `CONTEXT.md` or `Docs/adr/` don't exist yet, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## File structure

Single-context repo. Domain docs live at the root and under `Docs/`:

```
/
├── CLAUDE.md          ← current architecture reference (exists)
├── CONTEXT.md         ← glossary / ubiquitous language (created lazily)
├── Docs/
│   ├── adr/           ← architectural decision records (created lazily)
│   │   ├── 0001-....md
│   │   └── 0002-....md
│   └── GlyphsHeroDesign/   ← existing design docs
└── Assets/Code/       ← source (assembly-layered: Data → ... → GameLoop)
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `CLAUDE.md` / `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids — e.g. prefer the established names (`Pawn`, `ItemChain`, `ChainResolver`, `TetrisContainer`, `GamePhaseController`) over invented ones.

If the concept you need isn't documented yet, that's a signal — either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders) — but worth reopening because…_

# Night shift — unattended overnight runs

The **night runner** lets a sequence of headless Claude sessions work through the backlog while you're
AFK. This doc is the part that lives on `main` — the *knowledge* of the system. The *machinery* (runner,
prompts, hook) lives only on the **`night-base`** branch, so `main`/`laptopLab` stay free of tooling and
no SessionStart banner fires during normal interactive work.

## Two shifts, one backlog

- **Day shift** (you, interactively, on `main`/`laptopLab`) — **has priority and authority.** Curates the
  backlog and decides what the night runner is allowed to touch.
- **Night shift** (`Start-NightRun.ps1` on `night-base`) — unattended; only ever pulls work the day shift
  has explicitly cleared.

The single shared surface between them is **GitHub Issues** (`lolesch/glyphshero`, via `gh`) — it's
cross-branch by nature, so both shifts read and write the same queue without a file to sync.

| Concern | Where |
|---|---|
| Backlog / queue | GitHub Issues + triage labels (`Docs/agents/triage-labels.md`) |
| "Night-suitable, go" signal | the **`ready-for-agent`** label |
| What's already done | closed issues + per-issue night-runner comments + git history |
| Mid-task state *within* one night | `.claude/SESSION_HANDOVER.md` (on `night-base`/`night/<date>`) |
| Night output (code) | a `night/<date>` branch — never merged automatically |
| Morning summary | `Docs/agents/night-runs/<date>.md` on the night branch + issue comments |

## Day shift: curating the backlog (the authority bit)

The night runner is only as safe as the issues you clear for it. Stamp an issue **`ready-for-agent`** only
when it is genuinely **AFK-finishable** — the bar a task must clear:

- **Design-stable.** The *what* and *how* are decided; no open design fork. (Counter-example: "pool-
  generalise payload conditions" looked small but hid an undecided design — it became ADR-0006 instead.
  A task that needs a grilling/ADR session is **not** night-suitable.)
- **Bounded.** One coherent change that fits a single focused session, with a clear next step if larger.
- **Testable test-first.** There's a pure seam to red-green against (the `*Resolver` pattern), so the
  agent can prove the change — it **cannot** compile or run the Unity Test Runner (editor-only).
- **No interactive decision** needed mid-task — nothing only you can answer.
- **Self-contained context** — the issue body + `CONTEXT.md`/ADRs are enough; no tribal knowledge.

If a candidate fails any of these, label it `ready-for-human` or leave it `needs-triage` instead — keep it
out of the night queue. A candidate that fails specifically on **an open design fork** gets `needs-design`
(`Docs/agents/design-gate.md`), which is mutually exclusive with `ready-for-agent` — the runner's filter is
a whitelist, so a `needs-design` issue is simply invisible to it and never blocks the night. Order matters:
the runner takes the **lowest-numbered** open `ready-for-agent` issue, so number/prioritise accordingly.

## When the runner hits a design fork mid-slice (park, don't guess)

If, partway through a `ready-for-agent` issue, the agent reaches a **one-way-door** gap the design doesn't
answer (the ADR-0006 situation — a rule that's undefined or contradicts an accepted Decision), it must
neither silently fill it (drift) nor stall the whole night (blocked). Instead — **park it and move on**:

1. Commit only the already-decided **safe** part of the work.
2. Open a `needs-design` issue capturing the fork (what's undefined, options seen, why it's one-way).
3. Strip `ready-for-agent` from the original issue.
4. Pick up the **next** eligible `ready-for-agent` issue.

"Don't block the night" means *skip to the next funded issue*, never *guess to keep moving*. Every slice's
night-runner comment also carries the **slice ledger** (assumptions / decisions taken / gaps left open) per
`design-gate.md`. The runner's own instruction for this lives in `.claude/work-prompt.md` on `night-base`.

## Running a night (evening)

The harness lives on `night-base`, so check it out first:

```powershell
git checkout night-base
.\.claude\Start-NightRun.ps1      # stop early: create .claude\STOP  (or Ctrl-C)
```

The runner: refuses a dirty tree → syncs `night-base` to `origin/main` → loops fresh `claude -p` sessions,
each taking one `ready-for-agent` issue, doing one bounded chunk on `night/<date>`, committing, commenting
on the issue, and rewriting the handover. It stops when the queue is empty, quota is hit, `STOP` appears,
or `MaxIterations` is reached, then writes the morning summary. Requires `claude` + `gh` on PATH with
`gh auth` logged in.

## Morning: reviewing what the night did

Nothing reaches `main` on its own. To catch up:

```powershell
git checkout night/<date>        # the night's work, isolated
```

Read `Docs/agents/night-runs/<date>.md` for the high-level summary (systems-touched diagram + which
issues moved + what to verify). Then in Rider/Unity: **compile + run the Test Runner** (the agent could
not), confirm the `VERIFY:` notes, and merge only what you trust. Update the issues (close the genuinely
done ones; the agent only advanced/`label-cleared` them — it never closes, so a human verifies first).

## Boundaries (enforced by the harness)

- Never commits to `main` or `night-base`; work lands only on `night/<date>`.
- Runs with `--dangerously-skip-permissions` (so prompts don't block overnight) — broad tool access on
  this repo, which is exactly why everything is quarantined to a reviewable `night/<date>` branch.

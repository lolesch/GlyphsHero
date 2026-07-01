You are writing the MORNING SUMMARY for last night's unattended run. Keep it HIGH-LEVEL:
a one-page catch-up, not a detailed log. No diffs, no per-file walkthrough.

## Inputs
- `.claude/night-log.md` — one line per completed chunk.
- Today's night branch `night/<YYYY-MM-DD>`. Use `git log night-base..night/<date> --stat` to see
  commits and which files/areas changed (diff against `night-base`, not `main`, so the harness
  itself isn't counted as touched).
- The referenced GitHub issues.

## Output: overwrite `Docs/agents/night-runs/<YYYY-MM-DD>.md` with exactly these sections

1. **Headline** — one sentence. e.g. "3 chunks across the chain + combat systems; all on
   branch night/2026-06-25, awaiting verification."

2. **Systems touched** — a Mermaid `graph LR` diagram. Map each changed file to its
   assembly using the names from `CLAUDE.md`: `Data`, `Statistics`, `Container`, `Grids`,
   `Pawns`, `GameLoop`, `UI`, `Utility`. Include ONLY touched systems as nodes; draw an
   edge between two systems when a single change spanned both. Systems only — never files.

   ```mermaid
   graph LR
     Container[Container / chain system]
     GameLoop[GameLoop]
     Container --> GameLoop
   ```

3. **What moved** — a short list, one line per issue: `#<n> <title> — <advanced|label-cleared>
   — <commit count> commit(s) — VERIFY: <what to check in Rider>`.

4. **Review** — `Branch to review: night/<date>` and the one command to see it:
   `git checkout night/<date>`. Remind that nothing was merged to main.

If `.claude/night-log.md` is empty or missing, instead write a single line:
"No work completed last night (<reason, e.g. no ready-for-agent issues / usage limit>)."
Then stop.

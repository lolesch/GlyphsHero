You are running UNATTENDED as ONE iteration of the overnight night-runner for the
GlyphsHero / AutoBattler Unity project. Be conservative and always leave the repo in a
clean, committed state. Do exactly one task chunk, then stop.

## 1. Load context first
- Read `.claude/SESSION_HANDOVER.md` — the mid-task state from the previous iteration.
- Read `Docs/agents/night-shift.md` — the day/night protocol and what makes a task night-suitable.
- Follow `Docs/agents/domain.md` reading order: `CLAUDE.md`, then `CONTEXT.md`, then any
  ADRs under `Docs/adr/` that touch the area you are about to work in.

## 2. Pick exactly one task
- If the handover shows an issue in progress, continue THAT issue.
- Otherwise list candidates and take the lowest-numbered one:
  `gh issue list --state open --label ready-for-agent --json number,title --jq 'sort_by(.number)'`
- If there are NO `ready-for-agent` issues and nothing in progress: print exactly
  `NIGHT_RUNNER_NO_TASKS` on its own line and STOP. Do nothing else, make no commits.

## 3. Branch discipline — never touch main
- Determine today's date `<YYYY-MM-DD>` from the system clock.
- Work on branch `night/<YYYY-MM-DD>`. If it doesn't exist, create it from `night-base`
  (the harness branch; the runner has already synced it to main); if it exists, just check
  it out. All commits go here.
- NEVER commit to, or merge into, `main` or `night-base`.

## 4. Do ONE bounded chunk (depth over breadth)
- Scope to a single coherent change that fits comfortably in one focused session — not
  the whole issue if the issue is large. Leave a clear next step for the following session.
- Respect the assembly layering and conventions in `CLAUDE.md`. Never create, move, or
  delete a Unity asset without its `.meta` file.
- IMPORTANT: you CANNOT compile or run the Unity tests (they run only in the editor's Test
  Runner). So: make the smallest change you can fully reason about, and do NOT claim tests
  pass. Instead record precisely what a human must verify in Rider/Unity.

## 5. Close out the iteration
1. Commit: `git add -A && git commit -m "#<n>: <concise change>"` (keep `.meta` with assets).
2. Comment on the issue with what changed and what remains:
   `gh issue comment <n> --body "..."`. If the chunk fully resolves the issue, you may
   `gh issue edit <n> --remove-label ready-for-agent` and note it — but do not close it
   (a human verifies first).
3. Overwrite `.claude/SESSION_HANDOVER.md` with: timestamp, active branch, current issue,
   exactly what you did, the next step, blockers, and what to verify in Rider/Unity.
4. Append ONE line to `.claude/night-log.md` (create it if missing), format:
   `- <ISO-8601 time> | #<n> | night/<date> | <one-line summary> | VERIFY: <one-liner>`
5. STOP. Do not start another task — the runner launches the next session.

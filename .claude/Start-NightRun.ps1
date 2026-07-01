#Requires -Version 5.1
<#
  Start-NightRun.ps1 — overnight unattended work runner for the AutoBattler repo.

  WHAT IT DOES
    Launches a sequence of FRESH headless Claude Code sessions. Each session takes ONE
    `ready-for-agent` GitHub issue, does a single bounded chunk on a `night/<date>` branch,
    commits, updates SESSION_HANDOVER.md + night-log.md, and exits. The loop stops when
    there are no ready-for-agent issues left, Claude hits its usage limit, a STOP file
    appears, or the iteration cap is reached. Then it writes a one-page morning summary to
    Docs/agents/night-runs/<date>.md.

  RUN IT MANUALLY (evening) — you keep weekly-throttle control by choosing when to start.
  The harness lives ONLY on the `night-base` branch, so check it out first:
    cd C:\Users\loles\Desktop\LEONID\AutoBattler
    git checkout night-base
    .\.claude\Start-NightRun.ps1
    # stop early: create an empty file  .claude\STOP   (or press Ctrl-C)

  BACKLOG / COORDINATION (see Docs/agents/night-shift.md on main)
    - The queue is GitHub `ready-for-agent` issues — curated by the day shift (who has authority).
    - The night runner only ever picks `ready-for-agent`; it learns "what's done" from open/closed
      issues + git history, and reports back via `gh issue comment`.

  SAFETY NOTES
    - Refuses to run on a dirty tree (so interactive WIP is never entangled in an unattended run).
    - Syncs `night-base` to `main` at start, then forks `night/<date>` from it. Never commits to main;
      work lands only on night/<date> for you to review/merge.
    - Cannot compile or run Unity tests (editor-only) — the summary flags what to verify.
    - Runs Claude with --dangerously-skip-permissions so prompts don't block overnight.
      That grants the session broad tool access on THIS repo. Review before merging.
    - Requires `claude` and `gh` on PATH, and `gh auth` already logged in.
#>

param(
    [int]$MaxIterations = 12,                 # hard ceiling on work sessions per night
    [int]$PauseSeconds  = 20,                 # breather between sessions
    [int]$MaxTurns      = 40,                 # per-session turn ceiling passed to claude
    [string]$Model      = 'claude-sonnet-5',  # model for the headless sessions — Sonnet 5, not Opus (quota/cost)
    [switch]$SkipSummary                      # skip the morning-summary pass
)

$ErrorActionPreference = 'Stop'
$ClaudeDir  = $PSScriptRoot
$RepoRoot   = Split-Path -Parent $ClaudeDir          # .claude -> repo root
$StopFile   = Join-Path $ClaudeDir 'STOP'
$WorkPrompt = Join-Path $ClaudeDir 'work-prompt.md'
$SumPrompt  = Join-Path $ClaudeDir 'summary-prompt.md'
$RunLogDir  = Join-Path $ClaudeDir 'run-logs'
$Date       = Get-Date -Format 'yyyy-MM-dd'

New-Item -ItemType Directory -Force -Path $RunLogDir | Out-Null
Set-Location $RepoRoot

# --- Preconditions: clean tree, on the harness branch, synced to main ---------
# The harness lives only on night-base and work forks from it. Refuse to run on a
# dirty tree so an interactive WIP is never swept into an unattended run, then pull
# main forward so the night works on current code (merge, not rebase — no history
# rewrite on an unattended branch).
$dirty = (git status --porcelain)
if ($dirty) {
    Write-Host 'Working tree is not clean — commit or stash before a night run. Aborting.'
    exit 1
}
# Git writes branch/merge status to stderr ("Already on '...'", "Switched to...",
# "Already up to date"); under the script-level 'Stop' PS 5.1 turns that stderr into a
# terminating NativeCommandError. Drop to 'Continue' for the sync calls and gate on
# $LASTEXITCODE instead, then restore 'Stop' for the cmdlet-driven rest of the script.
$ErrorActionPreference = 'Continue'
git fetch origin --quiet 2>$null
git checkout night-base 2>$null | Out-Null
git merge --no-edit origin/main 2>$null | Out-Null
$mergeExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($mergeExit -ne 0) {
    Write-Host 'Could not sync night-base with origin/main (conflict?). Aborting.'
    git merge --abort 2>$null
    exit 1
}

# Output phrases that indicate the session ran out of quota. Kept tight on purpose —
# loose words ('out of', 'quota') match ordinary task output and stop the run early;
# MaxIterations is the real backstop.
$limitPatterns = @('usage limit', 'rate limit', '5-hour limit', 'limit reached',
                   'reached your usage')

function Invoke-ClaudeSession {
    param([string]$PromptPath, [string]$Tag)

    $prompt  = Get-Content -Raw $PromptPath
    $logFile = Join-Path $RunLogDir ("{0}_{1}_{2}.log" -f $Date, $Tag, (Get-Date -Format 'HHmmss'))
    Write-Host "[$(Get-Date -Format HH:mm:ss)] session ($Tag) -> $logFile"

    # --print = headless; stdin carries the prompt.
    # --dangerously-skip-permissions so it doesn't stall on a permission prompt overnight.
    # Local 'Continue': under PS 5.1 a native command's stderr captured via 2>&1 is wrapped as a
    # NativeCommandError; with the script-level 'Stop' that would terminate the run the moment claude
    # writes any stderr. We still want stderr in the log, just not as a fatal error.
    $ErrorActionPreference = 'Continue'
    $prompt | claude --print --model $Model --dangerously-skip-permissions --max-turns $MaxTurns 2>&1 |
        Tee-Object -FilePath $logFile | Out-Null

    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Text     = (Get-Content -Raw $logFile)
    }
}

Write-Host "=== Night run $Date  |  repo: $RepoRoot ==="
$completed = 0

for ($i = 1; $i -le $MaxIterations; $i++) {
    if (Test-Path $StopFile) { Write-Host 'STOP file present - halting.'; break }

    $r = Invoke-ClaudeSession -PromptPath $WorkPrompt -Tag ('work{0:00}' -f $i)

    if ($r.Text -match 'NIGHT_RUNNER_NO_TASKS') {
        Write-Host 'No ready-for-agent issues left - done.'; break
    }
    if ($r.ExitCode -ne 0 -or ($limitPatterns | Where-Object { $r.Text -match $_ })) {
        Write-Host "Stopping: usage limit hit or session error (exit $($r.ExitCode))."; break
    }

    $completed++
    if (Test-Path $StopFile) { Write-Host 'STOP file present - halting.'; break }
    Start-Sleep -Seconds $PauseSeconds
}

Write-Host "Completed $completed task session(s)."

if (-not $SkipSummary) {
    Write-Host 'Writing morning summary...'
    Invoke-ClaudeSession -PromptPath $SumPrompt -Tag 'summary' | Out-Null
}

Write-Host "=== Done. Read: Docs/agents/night-runs/$Date.md   (branch: night/$Date) ==="

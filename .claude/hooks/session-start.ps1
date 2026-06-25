# SessionStart hook — surfaces mid-task state + the domain reading order into context.
# Fires on every new/resumed session (interactive AND headless `claude -p`). Keep it fast.
$ErrorActionPreference = 'SilentlyContinue'

$claudeDir = Split-Path -Parent $PSScriptRoot          # .claude/hooks -> .claude
$handover  = Join-Path $claudeDir 'SESSION_HANDOVER.md'

if (Test-Path $handover) {
    Write-Output "=== SESSION_HANDOVER (mid-task state from last session) ==="
    Get-Content -Raw $handover
    Write-Output ""
}

Write-Output "Before working, follow Docs/agents/domain.md reading order: CLAUDE.md -> CONTEXT.md -> any relevant ADRs under Docs/adr/."

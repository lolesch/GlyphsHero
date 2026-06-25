# Night runs

One file per overnight unattended run (`YYYY-MM-DD.md`), written by the night runner
(`.claude/Start-NightRun.ps1`) at the end of each run.

Each file is a one-page, high-level catch-up: a Mermaid diagram of which **systems**
(assemblies) were touched, the issues that moved, and what to verify in Rider. It is not
a detailed changelog — the commits and issue comments hold the detail.

Work lands on a `night/<date>` branch; nothing is merged to `main` automatically. Review
the branch, compile + run the Test Runner in Rider/Unity, then merge what you trust.

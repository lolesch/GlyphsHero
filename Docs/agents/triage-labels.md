# Triage Labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in this repo's issue tracker.

| Label in mattpocock/skills | Label in our tracker | Meaning                                  |
| -------------------------- | -------------------- | ---------------------------------------- |
| `needs-triage`             | `needs-triage`       | Maintainer needs to evaluate this issue  |
| `needs-info`               | `needs-info`         | Waiting on reporter for more information |
| `ready-for-agent`          | `ready-for-agent`    | Fully specified, ready for an AFK agent  |
| `ready-for-human`          | `ready-for-human`    | Requires human implementation            |
| `wontfix`                  | `wontfix`            | Will not be actioned                     |
| _(local)_                  | `needs-design`       | Has an open one-way-door design fork; blocked on a decision (see below) |

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.

## `needs-design` (design-gate label)

Marks an issue with an unresolved **one-way-door** design fork — see `Docs/agents/design-gate.md`. Two rules:

- **Mutually exclusive with `ready-for-agent`.** An issue is never both. The night runner's filter is a
  *whitelist* (`ready-for-agent`), so a `needs-design` issue is invisible to it — this is what keeps the
  night shift unblocked. `needs-design` is a **day-shift** queue, not a runner blocker.
- **"Resolved" = a recorded decision**, not just a cleared label: the decision lands in `CONTEXT.md` (and a
  new / superseding ADR if it changes a frozen one), *then* the label clears and the issue may become
  `ready-for-agent`. Clearing the label without recording the decision is drift.

Edit the right-hand column to match whatever vocabulary you actually use.

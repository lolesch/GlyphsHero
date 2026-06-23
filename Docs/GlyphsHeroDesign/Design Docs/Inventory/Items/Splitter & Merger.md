---
tags:
  - Item
  - Attachment
  - Inventory
---

- **Bidirectional builds** — Amplifiers doing double duty in cycles

> **Role (ADR-0004 §4):** the Splitter is the **enabler of `Split` propagation** — it is what lets a delivery spawn **N parallel child deliveries** instead of one. Without a Splitter in the chain, a [[Payload]] adds a single child delivery node; with one, the impact fans out.

---

## Bidirectionality and Cycles

~~Chains are **bidirectional**. A weapon fires both arms simultaneously — each arm resolves independently as its own chain pass.~~
The Above would mean, that even without any attachment each weapon would fire twice per attack. Make this a splitter feature

**Cycles are allowed.** Resolution rule: a chain pass stops when it reaches the node that originally triggered it. Prevents infinite loops while preserving weapon triangles and similar configurations.

In a weapon triangle (A→B→C→A), Weapon A firing propagates through B and C before returning to A and stopping. B and C fire in payload mode — conditions checked, secondary effects contribute.

**Amplifier double duty:** An Amplifier between two weapons modifies whichever weapon is upstream relative to the current firing direction. In a cycle, the same Amplifier can modify different weapons depending on which trigger fires. Intentional depth — needs balance awareness.

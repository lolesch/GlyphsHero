using System;
using System.Collections.Generic;
using Code.Data.Enums;
using UnityEngine;

namespace Code.Data.Items.Weapon
{
    [Serializable]
    public sealed class PayloadBehavior
    {
        // The marginal cost of including this payload in the attack (ADR-0006 Decisions 4–5): authored as
        // a value + ModifierType so it flows through the same MutableFloat pipeline as any stat modifier
        // (FlatAdd / PercentAdd / PercentMult — PercentMult is the opt-in "deeper costs more"). The pure
        // PropagationCostResolver reads these to decide fail-forward propagation. Data is dependency-free,
        // so the runtime builds the Modifier from these primitives at fire time. Default 0 = free to add.
        [field: SerializeField] public float        CostValue { get; private set; }
        [field: SerializeField] public ModifierType CostType  { get; private set; } = ModifierType.FlatAdd;
        // The child delivery's pattern mask (stackable) and the Aoe radius it consumes. Payloads may
        // use Aoe (a disk) — weapons may not (see DeliveryPattern). ShapeSize is the Reach/shape-size
        // split: it is a pattern parameter, never the acquisition Reach.
        [field: SerializeField] public DeliveryPattern  Delivery           { get; private set; } = DeliveryPattern.Single;
        // Whose side this child delivery resolves against (ADR-0004 §3). Self = recoil onto the caster.
        [field: SerializeField] public Affinity         Affinity           { get; private set; } = Affinity.Hostile;
        // Where this child delivery centres (ADR-0004 §3) — independent of Affinity. Origin = a return /
        // self-anchored detonation on the firing pawn; Target = on the locked target (default).
        [field: SerializeField] public Anchor           Anchor             { get; private set; } = Anchor.Target;
        [field: SerializeField] public int              ShapeSize          { get; private set; } = 1;
        [field: SerializeField] public PayloadTiming    Timing             { get; private set; }
        [field: SerializeField] public float            TimingValue        { get; private set; }

        [SerializeReference]
        private List<PayloadEffect> _effects = new();
        public IReadOnlyList<PayloadEffect> Effects => _effects;
    }

    public enum PayloadTiming
    {
        Instant,
        Delayed,
        Repeating,
    }
}

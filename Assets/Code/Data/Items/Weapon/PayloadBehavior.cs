using System;
using System.Collections.Generic;
using Code.Data.Enums;
using UnityEngine;

namespace Code.Data.Items.Weapon
{
    [Serializable]
    public sealed class PayloadBehavior
    {
        [field: SerializeField] public ConditionType    Condition          { get; private set; }
        [field: SerializeField] public float            ConditionThreshold { get; private set; } = 0.5f;
        // The child delivery's pattern mask (stackable) and the Aoe radius it consumes. Payloads may
        // use Aoe (a disk) — weapons may not (see DeliveryPattern). ShapeSize is the Reach/shape-size
        // split: it is a pattern parameter, never the acquisition Reach.
        [field: SerializeField] public DeliveryPattern  Delivery           { get; private set; } = DeliveryPattern.Single;
        // Whose side this child delivery resolves against (ADR-0004 §3). Self = recoil onto the caster.
        [field: SerializeField] public Affinity         Affinity           { get; private set; } = Affinity.Hostile;
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

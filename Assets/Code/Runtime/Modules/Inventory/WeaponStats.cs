using Code.Data.Enums;

namespace Code.Runtime.Modules.Inventory
{
    /// <summary>
    /// The effective attack stats of a weapon after its chain modifiers have been applied —
    /// a computed value, not the weapon's live state. Produced by <see cref="WeaponStatResolver"/>;
    /// read by combat and the tooltip. The weapon's own MutableFloats are never mutated to answer
    /// "what are my stats?".
    /// </summary>
    public readonly struct WeaponStats
    {
        public float Damage       { get; }
        public float AttackSpeed  { get; }
        public float ResourceCost { get; }
        /// <summary>Which pool the weapon spends from (ADR-0005 §2). A Converter reclassifies this via ConverterAxis.Resource.</summary>
        public ResourceType    CostResource { get; }
        /// <summary>The resolved delivery pattern mask — reclassified by a Delivery Converter (ADR-0004 §1).</summary>
        public DeliveryPattern Delivery     { get; }
        /// <summary>Whose side this delivery resolves against (ADR-0004 §3) — reclassified by an Affinity Converter.</summary>
        public Affinity        Affinity     { get; }
        /// <summary>What the delivery's geometry centres on (ADR-0004 §3), independent of Affinity — reclassified by an Anchor Converter.</summary>
        public Anchor          Anchor       { get; }

        public WeaponStats(float damage, float attackSpeed, float resourceCost, ResourceType costResource,
            DeliveryPattern delivery, Affinity affinity, Anchor anchor)
        {
            Damage       = damage;
            AttackSpeed  = attackSpeed;
            ResourceCost = resourceCost;
            CostResource = costResource;
            Delivery     = delivery;
            Affinity     = affinity;
            Anchor       = anchor;
        }
    }
}

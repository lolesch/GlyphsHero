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
        public float Damage           { get; }
        public float AttackSpeed      { get; }
        public float ResourceCost     { get; }
        public float ResourceGenOnHit { get; }
        /// <summary>The resolved delivery pattern mask (a Converter would reclassify it here later).</summary>
        public DeliveryPattern Delivery { get; }
        /// <summary>Whose side this delivery resolves against (ADR-0004 §3); a Converter would reclassify it here later.</summary>
        public Affinity Affinity { get; }

        public WeaponStats(float damage, float attackSpeed, float resourceCost, float resourceGenOnHit, DeliveryPattern delivery, Affinity affinity)
        {
            Damage           = damage;
            AttackSpeed      = attackSpeed;
            ResourceCost     = resourceCost;
            ResourceGenOnHit = resourceGenOnHit;
            Delivery         = delivery;
            Affinity         = affinity;
        }
    }
}

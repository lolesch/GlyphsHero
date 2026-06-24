namespace Code.Data.Enums
{
    /// <summary>
    /// Whose occupancy a delivery counts — the Affinity axis (ADR-0004 §3), promoted out of the old
    /// <c>DeliveryPattern.Self</c> flag. <see cref="Hostile"/> resolves against the enemy team;
    /// <see cref="Friendly"/> and <see cref="Self"/> resolve against the caster's own side.
    ///
    /// <see cref="Friendly"/> and <see cref="Self"/> are intentionally separate values:
    /// <see cref="Friendly"/> hits allies <em>excluding</em> the caster; <see cref="Self"/> hits only
    /// the caster. An "all-friendlies-including-self" effect needs two deliveries (one Friendly, one
    /// Self). A <c>[Flags]</c> upgrade would let callers combine them, but <see cref="Hostile"/> is
    /// currently 0 — promoting to power-of-two values would break serialised asset data.
    ///
    /// One affinity per delivery: a "recoil" (hit an enemy <em>and</em> hurt yourself) is expressed as a
    /// self-affinity payload child delivery. Independent of the delivery <see cref="DeliveryPattern"/>
    /// (geometry) and <see cref="Anchor"/> (where the geometry centres).
    /// </summary>
    public enum Affinity
    {
        /// <summary>Default (0) — hits the enemy team standing on the covered hexes.</summary>
        Hostile,

        /// <summary>Hits the caster's allies, <em>not</em> the caster itself — heals/buffs.</summary>
        Friendly,

        /// <summary>Hits only the caster — recoil or self-buff build-around.</summary>
        Self,
    }
}

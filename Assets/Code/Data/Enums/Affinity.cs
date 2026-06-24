namespace Code.Data.Enums
{
    /// <summary>
    /// Whose occupancy a delivery counts — the Affinity axis (ADR-0004 §3), promoted out of the old
    /// <c>DeliveryPattern.Self</c> flag. <see cref="Hostile"/> resolves against the enemy team;
    /// <see cref="Friendly"/> and <see cref="Self"/> resolve against the caster's own side.
    ///
    /// One affinity per delivery: a "recoil" (hit an enemy <em>and</em> hurt yourself) is expressed as a
    /// self-affinity payload child delivery, not a stacked flag. Independent of the delivery
    /// <see cref="DeliveryPattern"/> (geometry) and Anchor (where the geometry centres).
    /// </summary>
    public enum Affinity
    {
        /// <summary>Default — hits the enemy team standing on the covered hexes.</summary>
        Hostile,

        /// <summary>Hits the caster's own team — heals/buffs. Content pending (aura/buff work).</summary>
        Friendly,

        /// <summary>Hits the caster itself — the deliberate self-hurt build-around. Self-anchored (v1).</summary>
        Self,
    }
}

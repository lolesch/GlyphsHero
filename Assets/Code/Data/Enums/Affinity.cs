using System;

namespace Code.Data.Enums
{
    /// <summary>
    /// Whose occupancy a delivery counts — the Affinity axis (ADR-0004 §3), promoted out of the old
    /// <c>DeliveryPattern.Self</c> flag.
    ///
    /// Values are powers of two so callers can combine them: <c>Friendly | Self</c> hits all allies
    /// including the caster; <c>Hostile | Self</c> is a recoil that damages both an enemy and yourself.
    /// Composite affinities require the combat side to call <see cref="DeliveryAffinity"/> methods
    /// per-bit — <see cref="ResolveTargets"/> in <c>PawnCombatController</c> currently handles only
    /// single-bit affinities (v1 limitation, see TODO there).
    ///
    /// Independent of the delivery <see cref="DeliveryPattern"/> (geometry) and
    /// <see cref="Anchor"/> (where the geometry centres).
    /// </summary>
    [Flags]
    public enum Affinity
    {
        /// <summary>Zero — no affinity. Not a valid delivery value; used as a default/sentinel only.</summary>
        None = 0,

        /// <summary>Hits the enemy team standing on the covered hexes.</summary>
        Hostile = 1 << 0,

        /// <summary>Hits the caster's allies, <em>not</em> the caster itself — heals/buffs.</summary>
        Friendly = 1 << 1,

        /// <summary>Hits only the caster — recoil or self-buff build-around.</summary>
        Self = 1 << 2,
    }
}

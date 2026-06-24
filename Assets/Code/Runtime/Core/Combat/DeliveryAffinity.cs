using Code.Data.Enums;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Pure rule for the Affinity axis (ADR-0004 §3): whose side a delivery resolves against. No pawns,
    /// registry, or engine, so it is unit-testable in isolation. Where the geometry <em>centres</em> is
    /// now the independent Anchor axis (<see cref="DeliveryAnchor"/>), no longer derived from affinity.
    /// </summary>
    public static class DeliveryAffinity
    {
        /// <summary>True when any bit in the affinity resolves against the caster's own side.</summary>
        public static bool TargetsCasterSide(Affinity affinity)
            => (affinity & (Affinity.Friendly | Affinity.Self)) != 0;

        /// <summary>True when any bit in the affinity resolves against the enemy team.</summary>
        public static bool TargetsEnemySide(Affinity affinity)
            => (affinity & Affinity.Hostile) != 0;
    }
}

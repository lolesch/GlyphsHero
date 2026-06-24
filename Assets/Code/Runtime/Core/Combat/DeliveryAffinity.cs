using Code.Data.Enums;
using Submodules.Utility.Extensions;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Pure rules for the Affinity axis (ADR-0004 §3): whose side a delivery resolves against, and —
    /// as a v1 simplification — where its geometry anchors. No pawns, registry, or engine, so it is
    /// unit-testable in isolation.
    ///
    /// <b>v1 anchor coupling:</b> a <see cref="Affinity.Self"/> delivery is self-anchored (centres on
    /// the firing pawn); <see cref="Affinity.Hostile"/>/<see cref="Affinity.Friendly"/> anchor on the
    /// chosen target. The fully independent Anchor axis (anchor-self for any affinity, e.g. a
    /// heal-around-me) is deferred (ADR-0004 §3).
    /// </summary>
    public static class DeliveryAffinity
    {
        /// <summary>
        /// True when the delivery resolves against the caster's <b>own side</b> (friendly or self)
        /// rather than the enemy team.
        /// </summary>
        public static bool TargetsCasterSide(Affinity affinity) => affinity != Affinity.Hostile;

        /// <summary>Where the delivery's geometry centres (see the v1 anchor coupling above).</summary>
        public static Hex Anchor(Hex origin, Hex target, Affinity affinity)
            => affinity == Affinity.Self ? origin : target;
    }
}

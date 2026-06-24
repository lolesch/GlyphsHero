using Code.Data.Enums;
using Submodules.Utility.Extensions;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Pure rule for the Anchor axis (ADR-0004 §3): where a delivery's geometry centres. Independent of
    /// the <see cref="Affinity"/> axis (<see cref="DeliveryAffinity"/>) — anchor-Origin combines with any
    /// affinity, so a damage nova, a heal-around-me, and a self-hurt proc are all the same anchor choice
    /// over different affinities. No pawns, registry, or engine, so it is unit-testable in isolation.
    /// </summary>
    public static class DeliveryAnchor
    {
        /// <summary>
        /// The hex a delivery's geometry centres on: the chosen <paramref name="target"/> for
        /// <see cref="Anchor.Target"/>, the firing pawn's own <paramref name="origin"/> for
        /// <see cref="Anchor.Origin"/>.
        /// </summary>
        public static Hex Resolve(Hex origin, Hex target, Anchor anchor)
            => anchor == Anchor.Origin ? origin : target;
    }
}

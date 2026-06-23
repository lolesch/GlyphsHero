using System.Collections.Generic;
using Code.Data.Enums;
using Submodules.Utility.Extensions;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Pure geometry for the Delivery Pattern axis (ADR-0002, CONTEXT.md). Maps a firing
    /// <paramref name="origin"/>, an aim <paramref name="anchor"/>, and a stackable
    /// <see cref="DeliveryPattern"/> mask to the set of <b>covered hexes</b> — the hexes a delivery
    /// affects. Damage is then resolved by occupancy of this set (every hostile pawn standing on a
    /// covered hex is hit), which is the caller's concern; this stays a pure function with no pawns,
    /// registry, grid, or engine dependency, so it is unit-testable in isolation.
    ///
    /// The mask is a union: each set flag contributes its hexes and overlaps are de-duplicated. The
    /// size-free patterns derive their footprint from the origin→anchor geometry alone (Reach gates
    /// acquisition upstream); only <see cref="DeliveryPattern.Aoe"/> consumes
    /// <paramref name="shapeSize"/>.
    /// </summary>
    public static class DeliveryResolver
    {
        /// <summary>
        /// The flags whose covered hexes resolve against the caster's <b>own side</b> rather than
        /// hostiles — the friendly/self affinity of the delivery axis. Today only <see cref="DeliveryPattern.Self"/>
        /// (the firing pawn's own hex); the future aura/buff work extends this set.
        /// </summary>
        public const DeliveryPattern SelfAffinity = DeliveryPattern.Self;

        /// <summary>
        /// The <b>self-affinity</b> subset of the footprint: the covered hexes a delivery resolves
        /// against the caster's own team instead of hostiles (ADR-0003). A <see cref="DeliveryPattern.Self"/>
        /// firing returns the origin hex so the deliberate self-hurt build-around hits the firing pawn —
        /// hostile occupancy can't express it, since no enemy ever stands on the caster's hex. Empty for a
        /// purely hostile mask. The hostile-affinity hexes remain the rest of <see cref="CoveredHexes"/>.
        /// </summary>
        public static IReadOnlyList<Hex> SelfHexes(Hex origin, Hex anchor, DeliveryPattern pattern, int shapeSize = 0)
            => CoveredHexes(origin, anchor, pattern & SelfAffinity, shapeSize);

        public static IReadOnlyList<Hex> CoveredHexes(Hex origin, Hex anchor, DeliveryPattern pattern, int shapeSize = 0)
        {
            var covered = new HashSet<Hex>();

            if ((pattern & DeliveryPattern.Single) != 0) covered.Add(anchor);
            if ((pattern & DeliveryPattern.Self)   != 0) covered.Add(origin);
            if ((pattern & DeliveryPattern.Line)   != 0) AddLine(covered, origin, anchor);
            if ((pattern & DeliveryPattern.Cleave) != 0) AddCleave(covered, origin, anchor);
            if ((pattern & DeliveryPattern.Aoe)    != 0) AddAoe(covered, anchor, shapeSize);

            return new List<Hex>(covered);
        }

        /// <summary>A beam: every hex from the firing pawn out to the anchor, the pawn's own hex excluded.</summary>
        private static void AddLine(HashSet<Hex> covered, Hex origin, Hex anchor)
        {
            foreach (var hex in origin.HexLine(anchor))
                if (hex != origin)
                    covered.Add(hex);
        }

        /// <summary>
        /// A 3-hex arc: the anchor plus the two of its six neighbours that sit on the <b>same ring</b>
        /// (same distance from the origin). Every hex on a ring has exactly two same-ring neighbours,
        /// so this is always the anchor + 2 flanks — no angle, facing, or radius parameter, and
        /// diagonal anchors resolve for free. It narrows with distance (3 of 6 hexes on ring 1, a thin
        /// slice of ring 3).
        /// </summary>
        private static void AddCleave(HashSet<Hex> covered, Hex origin, Hex anchor)
        {
            covered.Add(anchor);

            var ring = origin.Distance(anchor);
            foreach (var neighbour in anchor.Neighbors())
                if (origin.Distance(neighbour) == ring)
                    covered.Add(neighbour);
        }

        /// <summary>A disk of <paramref name="radius"/> around the anchor (payload-only blast).</summary>
        private static void AddAoe(HashSet<Hex> covered, Hex anchor, int radius)
        {
            foreach (var hex in anchor.HexRange(radius))
                covered.Add(hex);
        }
    }
}

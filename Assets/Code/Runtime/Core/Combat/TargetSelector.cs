using System.Collections.Generic;
using Code.Data.Enums;
using Code.Runtime.Pawns;
using Submodules.Utility.Extensions;

namespace Code.Runtime.Core.Combat
{
    
    /// <summary>
    /// Returns the nearest candidate within weapon range. Returns null if all are out of range.
    /// </summary>
    public static class TargetSelector //: ITargetSelector
    {
    // TODO: if this checks for pawns in weapon range, how does the pawn knows where to move to???
    // shouldn't it be FindTarget and then subtract the weapon range from the path?
        public static IPawn Select(IPawn attacker, IReadOnlyList<IPawn> candidates, int maxRange)
        {
            IPawn nearest  = null;
            var         bestDist = int.MaxValue;

            foreach (var candidate in candidates)
            {
                var dist = attacker.HexPosition.Distance(candidate.HexPosition);
                if (dist > maxRange || dist >= bestDist) continue;
                bestDist = dist;
                nearest  = candidate;
            }

            return nearest;
        }
        
        public static IEnumerable<IPawn>GetPawnsInRange(Hex center, int range, IEnumerable<IPawn> occupants, PawnTeam? filter = null)
        {
            var hexesInRange = new HashSet<Hex>(center.HexRange(range));
            foreach (var occupant in occupants)
            {
                if (!hexesInRange.Contains(occupant.HexPosition)) continue;
                if (occupant.Team != filter) continue;
                yield return occupant;
            }
        }

        /// <summary>
        /// Every occupant standing on one of the <paramref name="coveredHexes"/> — the hex-occupancy
        /// damage rule (ADR-0002). The covered set is the output of <see cref="DeliveryResolver"/>;
        /// damage hits whoever stands on it, the aim anchor only shaped it. Optionally filtered to one
        /// team (no filter = all teams, unlike <see cref="GetPawnsInRange"/>).
        /// </summary>
        public static IEnumerable<IPawn> PawnsOnHexes(IEnumerable<Hex> coveredHexes, IEnumerable<IPawn> occupants, PawnTeam? filter = null)
        {
            var covered = coveredHexes as HashSet<Hex> ?? new HashSet<Hex>(coveredHexes);
            foreach (var occupant in occupants)
            {
                if (!covered.Contains(occupant.HexPosition)) continue;
                if (filter.HasValue && occupant.Team != filter.Value) continue;
                yield return occupant;
            }
        }
    }

    //public interface ITargetSelector
    //{
    //    public IEnumerable<IPawn> GetPawnsInRange(Hex center, int range, HashSet<IPawn> occupants);
    //    IPawn Select(IPawn attacker, IReadOnlyList<IPawn> candidates, int maxRange);
    //}
}
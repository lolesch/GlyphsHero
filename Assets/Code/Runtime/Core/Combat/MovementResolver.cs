using System.Collections.Generic;
using Submodules.Utility.Extensions;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// A single pawn's movement state entering a resolution tick. The next step toward the target
    /// is precomputed (single-unit A* against the frozen snapshot) and fed in, so the resolver
    /// itself stays a pure function with no grid, pathfinder, or engine dependency.
    /// </summary>
    public struct Mover
    {
        /// <summary>Stable, deterministic key (registration index) — the contested-hex tiebreak.</summary>
        public int   Id;
        public Hex   Position;
        /// <summary>Hex the pawn is closing on (its approach target). May be <see cref="Hex.Invalid"/>.</summary>
        public Hex   Target;
        /// <summary>Minimum active-weapon reach: the pawn stops closing once within this ring.</summary>
        public int   Reach;
        /// <summary>Precomputed next hex toward <see cref="Target"/>, or <see cref="Hex.Invalid"/> if blocked/no path.</summary>
        public Hex   NextStep;
        /// <summary>Terrain cost of entering <see cref="NextStep"/> — must be banked before stepping.</summary>
        public int   NextStepCost;
        /// <summary>Move-readiness banked so far.</summary>
        public float Readiness;
        /// <summary>Readiness gained this tick (movement speed × tick interval).</summary>
        public float ReadinessGain;
    }

    /// <summary>Per-mover outcome of a resolution tick.</summary>
    public struct MoveResult
    {
        public int   Id;
        public Hex   Position;
        public float Readiness;
        public bool  Stepped;
    }

    /// <summary>
    /// Pure per-tick movement step-rule (ADR-0001, Decisions 3-5, 7). Read-then-write within a tick:
    /// every mover's intent is gathered against the frozen snapshot, then contested hexes are
    /// arbitrated and applied together — so no mover decides against another's mid-tick move.
    ///
    /// Each mover accrues readiness; a mover steps only when it has banked the next hex's terrain
    /// cost (carrying surplus). A mover already within reach, blocked (no next step), or out-banked
    /// idles. When several ready movers want the same hex, the one that would be nearest its own
    /// target wins (ties by stable id); losers idle and keep their banked readiness to retry.
    /// </summary>
    public static class MovementResolver
    {
        public static IReadOnlyList<MoveResult> Resolve(IReadOnlyList<Mover> movers)
        {
            var results = new MoveResult[movers.Count];

            // Group ready movers by the hex they want, so contention can be arbitrated as a batch.
            var contenders = new Dictionary<Hex, List<int>>();

            for (var i = 0; i < movers.Count; i++)
            {
                var m              = movers[i];
                var bankedReadiness = m.Readiness + m.ReadinessGain;

                // Default outcome: accrue readiness but stay put.
                results[i] = new MoveResult
                {
                    Id        = m.Id,
                    Position  = m.Position,
                    Readiness = bankedReadiness,
                    Stepped   = false,
                };

                var inReach = m.Target.IsValid && m.Position.Distance(m.Target) <= m.Reach;
                var hasStep = m.NextStep.IsValid && m.NextStep != m.Position;
                var ready   = bankedReadiness >= m.NextStepCost;

                if (inReach || !hasStep || !ready)
                    continue;

                if (!contenders.TryGetValue(m.NextStep, out var list))
                    contenders[m.NextStep] = list = new List<int>();
                list.Add(i);
            }

            foreach (var (hex, indices) in contenders)
            {
                var winner = SelectWinner(movers, indices);
                var m      = movers[winner];

                results[winner] = new MoveResult
                {
                    Id        = m.Id,
                    Position  = hex,
                    Readiness = m.Readiness + m.ReadinessGain - m.NextStepCost,
                    Stepped   = true,
                };
                // Losers keep the idle result assigned above (banked readiness retained).
            }

            return results;
        }

        /// <summary>
        /// Closest-to-target wins (the more committed pawn): minimal distance from the contested hex
        /// to the contender's own target, ties broken by the smaller stable id for determinism.
        /// </summary>
        private static int SelectWinner(IReadOnlyList<Mover> movers, List<int> indices)
        {
            var best         = indices[0];
            var bestDistance = movers[best].NextStep.Distance(movers[best].Target);

            for (var k = 1; k < indices.Count; k++)
            {
                var idx      = indices[k];
                var distance = movers[idx].NextStep.Distance(movers[idx].Target);

                if (distance < bestDistance || (distance == bestDistance && movers[idx].Id < movers[best].Id))
                {
                    best         = idx;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}

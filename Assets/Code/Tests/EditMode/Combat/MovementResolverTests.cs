using System.Collections.Generic;
using System.Linq;
using Code.Runtime.Core.Combat;
using FluentAssertions;
using NUnit.Framework;
using Submodules.Utility.Extensions;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Locks the pure per-tick movement step-rule (ADR-0001, Decisions 3-5, 7). The resolver is the
    /// read-then-write core of resolution movement: given a frozen snapshot of movers (each with its
    /// precomputed next step toward its target), it accrues move-readiness, gates stepping by speed
    /// vs terrain cost, resolves contested hexes by closest-to-target (ties by stable id), and idles
    /// anyone blocked or already in reach. Pathfinding is fed in as <c>NextStep</c> so this stays a
    /// pure function with no grid, MonoBehaviour, or Timer.
    /// </summary>
    [TestFixture]
    public sealed class MovementResolverTests
    {
        private static Mover Mover(int id, Hex pos, Hex target, Hex nextStep,
            float readiness = 0f, float gain = 1f, int stepCost = 1, int reach = 1) => new()
        {
            Id           = id,
            Position     = pos,
            Target       = target,
            Reach        = reach,
            NextStep     = nextStep,
            NextStepCost = stepCost,
            Readiness    = readiness,
            ReadinessGain = gain,
        };

        private static MoveResult ResultFor(IReadOnlyList<MoveResult> results, int id) =>
            results.Single(r => r.Id == id);

        [Test]
        public void ReadyMover_WithClearStep_Steps()
        {
            var from = new Hex(0, 0);
            var to   = new Hex(1, 0);
            var results = MovementResolver.Resolve(new[]
            {
                Mover(1, from, new Hex(5, 0), nextStep: to, readiness: 0f, gain: 1f, stepCost: 1),
            });

            var r = ResultFor(results, 1);
            r.Stepped.Should().BeTrue();
            r.Position.Should().Be(to);
        }

        [Test]
        public void Readiness_GatesStepRateBySpeed()
        {
            // gain 0.5/tick, step cost 1 → not ready on tick one, ready on tick two.
            var from = new Hex(0, 0);
            var to   = new Hex(1, 0);

            var afterTick1 = ResultFor(MovementResolver.Resolve(new[]
            {
                Mover(1, from, new Hex(5, 0), nextStep: to, readiness: 0f, gain: 0.5f, stepCost: 1),
            }), 1);

            afterTick1.Stepped.Should().BeFalse("0.5 banked < cost 1");
            afterTick1.Position.Should().Be(from);
            afterTick1.Readiness.Should().BeApproximately(0.5f, 1e-4f);

            var afterTick2 = ResultFor(MovementResolver.Resolve(new[]
            {
                Mover(1, from, new Hex(5, 0), nextStep: to, readiness: afterTick1.Readiness, gain: 0.5f, stepCost: 1),
            }), 1);

            afterTick2.Stepped.Should().BeTrue("1.0 banked >= cost 1");
            afterTick2.Position.Should().Be(to);
        }

        [Test]
        public void Step_CarriesReadinessSurplus()
        {
            var from = new Hex(0, 0);
            var to   = new Hex(1, 0);
            var r = ResultFor(MovementResolver.Resolve(new[]
            {
                // 1.5 banked, cost 1 → step and carry 0.5.
                Mover(1, from, new Hex(5, 0), nextStep: to, readiness: 0.5f, gain: 1f, stepCost: 1),
            }), 1);

            r.Stepped.Should().BeTrue();
            r.Readiness.Should().BeApproximately(0.5f, 1e-4f);
        }

        [Test]
        public void HighTerrainCost_HoldsUntilBanked()
        {
            var from = new Hex(0, 0);
            var to   = new Hex(1, 0);
            var r = ResultFor(MovementResolver.Resolve(new[]
            {
                // cost 3, only 1 banked this tick → idle, keep readiness.
                Mover(1, from, new Hex(5, 0), nextStep: to, readiness: 0f, gain: 1f, stepCost: 3),
            }), 1);

            r.Stepped.Should().BeFalse();
            r.Position.Should().Be(from);
            r.Readiness.Should().BeApproximately(1f, 1e-4f);
        }

        [Test]
        public void BlockedMover_NoNextStep_Idles()
        {
            var from = new Hex(0, 0);
            var r = ResultFor(MovementResolver.Resolve(new[]
            {
                Mover(1, from, new Hex(5, 0), nextStep: Hex.Invalid, readiness: 5f, gain: 1f, stepCost: 1),
            }), 1);

            r.Stepped.Should().BeFalse();
            r.Position.Should().Be(from);
        }

        [Test]
        public void MoverAlreadyWithinReach_DoesNotStep()
        {
            var from   = new Hex(0, 0);
            var target = new Hex(2, 0); // distance 2, reach 2 → in reach, should engage not move
            var r = ResultFor(MovementResolver.Resolve(new[]
            {
                Mover(1, from, target, nextStep: new Hex(1, 0), readiness: 5f, gain: 1f, stepCost: 1, reach: 2),
            }), 1);

            r.Stepped.Should().BeFalse();
            r.Position.Should().Be(from);
        }

        [Test]
        public void ContestedHex_ClosestToTargetWins_LoserIdles()
        {
            var contested = new Hex(0, 0);

            // Both are out of reach (so both genuinely seek the hex), but from the contested hex
            // mover 2's target is nearer (dist 2) than mover 1's (dist 5) → mover 2 is the more
            // committed pawn and wins; mover 1 idles.
            var movers = new[]
            {
                Mover(1, new Hex(-1, 0), target: new Hex(-5, 0), nextStep: contested, readiness: 5f),
                Mover(2, new Hex(1, 0),  target: new Hex(0, 2),  nextStep: contested, readiness: 5f),
            };

            var results = MovementResolver.Resolve(movers);

            ResultFor(results, 2).Stepped.Should().BeTrue();
            ResultFor(results, 2).Position.Should().Be(contested);

            var loser = ResultFor(results, 1);
            loser.Stepped.Should().BeFalse("only one pawn may take the contested hex");
            loser.Position.Should().Be(new Hex(-1, 0));
            loser.Readiness.Should().BeApproximately(6f, 1e-4f, "loser keeps banked readiness to retry next tick");
        }

        [Test]
        public void ContestedHex_EqualDistance_LowerIdWins_Deterministic()
        {
            var contested = new Hex(0, 0);
            var target    = new Hex(0, 3); // symmetric: both contenders equidistant from this target hex

            var movers = new[]
            {
                Mover(7, new Hex(-1, 0), target: target, nextStep: contested, readiness: 5f),
                Mover(3, new Hex(1, 0),  target: target, nextStep: contested, readiness: 5f),
            };

            var results = MovementResolver.Resolve(movers);

            ResultFor(results, 3).Stepped.Should().BeTrue("lower id wins the tie");
            ResultFor(results, 7).Stepped.Should().BeFalse();
        }

        [Test]
        public void TwoConvergingPawns_NeverShareAHex()
        {
            var contested = new Hex(0, 0);
            var movers = new[]
            {
                Mover(1, new Hex(-1, 0), target: new Hex(2, 0),  nextStep: contested, readiness: 5f),
                Mover(2, new Hex(1, 0),  target: new Hex(-2, 0), nextStep: contested, readiness: 5f),
            };

            var results = MovementResolver.Resolve(movers);

            var occupied = results.Select(r => r.Position).ToList();
            occupied.Should().OnlyHaveUniqueItems("no two pawns may occupy the same hex after a tick");
        }

        [Test]
        public void DistinctDestinations_BothStep()
        {
            var movers = new[]
            {
                Mover(1, new Hex(0, 0), target: new Hex(5, 0),  nextStep: new Hex(1, 0),  readiness: 5f),
                Mover(2, new Hex(0, 5), target: new Hex(0, -5), nextStep: new Hex(0, 4),  readiness: 5f),
            };

            var results = MovementResolver.Resolve(movers);

            ResultFor(results, 1).Stepped.Should().BeTrue();
            ResultFor(results, 2).Stepped.Should().BeTrue();
        }
    }
}

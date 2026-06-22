using Code.Data.Enums;
using Code.Runtime.Core.Combat;
using FluentAssertions;
using NUnit.Framework;
using Submodules.Utility.Extensions;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Locks the pure Delivery Pattern geometry (ADR-0002, CONTEXT.md). Given a firing origin, an aim
    /// anchor, and a stackable <see cref="DeliveryPattern"/> mask, <see cref="DeliveryResolver"/>
    /// returns the covered hexes (the occupancy set damage resolves on). Geometry only — no pawns,
    /// registry, or grid — so the shape of every pattern, and the union of stacked patterns, is
    /// pinned here in isolation. All cases fire from <see cref="Origin"/> facing east.
    /// </summary>
    [TestFixture]
    public sealed class DeliveryResolverTests
    {
        private static readonly Hex Origin = new(0, 0);

        private static System.Collections.Generic.IReadOnlyList<Hex> Cover(
            DeliveryPattern pattern, Hex anchor, int shapeSize = 0) =>
            DeliveryResolver.CoveredHexes(Origin, anchor, pattern, shapeSize);

        [Test]
        public void None_CoversNothing()
        {
            Cover(DeliveryPattern.None, new Hex(2, 0)).Should().BeEmpty();
        }

        [Test]
        public void Single_CoversAnchorOnly()
        {
            Cover(DeliveryPattern.Single, new Hex(3, 0))
                .Should().BeEquivalentTo(new[] { new Hex(3, 0) }, o => o.ComparingByValue<Hex>());
        }

        [Test]
        public void Self_CoversOriginOnly()
        {
            // Anchor is far away, but Self covers the firing pawn's own hex.
            Cover(DeliveryPattern.Self, new Hex(3, 0))
                .Should().BeEquivalentTo(new[] { Origin }, o => o.ComparingByValue<Hex>());
        }

        [Test]
        public void Line_CoversPathToAnchor_ExcludingOrigin()
        {
            Cover(DeliveryPattern.Line, new Hex(3, 0))
                .Should().BeEquivalentTo(
                    new[] { new Hex(1, 0), new Hex(2, 0), new Hex(3, 0) },
                    o => o.ComparingByValue<Hex>());
        }

        [Test]
        public void Cleave_Ring1_CoversAnchorAndTwoSameRingNeighbours()
        {
            Cover(DeliveryPattern.Cleave, new Hex(1, 0))
                .Should().BeEquivalentTo(
                    new[] { new Hex(1, 0), new Hex(1, -1), new Hex(0, 1) },
                    o => o.ComparingByValue<Hex>());
        }

        [Test]
        public void Cleave_Ring2_CoversAnchorAndTwoSameRingNeighbours()
        {
            Cover(DeliveryPattern.Cleave, new Hex(2, 0))
                .Should().BeEquivalentTo(
                    new[] { new Hex(2, 0), new Hex(2, -1), new Hex(1, 1) },
                    o => o.ComparingByValue<Hex>());
        }

        [Test]
        public void Cleave_DiagonalAnchor_ResolvesWithoutFacingMath()
        {
            // A non-corner ring-2 anchor: still anchor + its two same-ring neighbours, no facing enum.
            Cover(DeliveryPattern.Cleave, new Hex(2, -1))
                .Should().BeEquivalentTo(
                    new[] { new Hex(2, -1), new Hex(2, -2), new Hex(2, 0) },
                    o => o.ComparingByValue<Hex>());
        }

        [Test]
        public void Cleave_ExcludesNeighboursOffTheRing()
        {
            // For anchor (2,0): the inward neighbour (1,0) [ring 1] and outward (3,0) [ring 3] must
            // NOT be covered — only same-distance flanks. Guards the same-ring filter against a flip.
            var covered = Cover(DeliveryPattern.Cleave, new Hex(2, 0));

            covered.Should().HaveCount(3);
            covered.Should().NotContain(new Hex(1, 0));
            covered.Should().NotContain(new Hex(3, 0));
        }

        [Test]
        public void Aoe_CoversDiskAroundAnchor()
        {
            // Radius-1 disk around the anchor = anchor + its six neighbours.
            Cover(DeliveryPattern.Aoe, new Hex(0, 0), shapeSize: 1)
                .Should().BeEquivalentTo(
                    new[]
                    {
                        new Hex(0, 0),
                        new Hex(1, 0), new Hex(-1, 0),
                        new Hex(0, 1), new Hex(0, -1),
                        new Hex(1, -1), new Hex(-1, 1),
                    },
                    o => o.ComparingByValue<Hex>());
        }

        [Test]
        public void StackedPatterns_UnionTheirHexes()
        {
            // Line | Cleave at anchor (2,0): line {(1,0),(2,0)} ∪ cleave {(2,0),(2,-1),(1,1)}.
            Cover(DeliveryPattern.Line | DeliveryPattern.Cleave, new Hex(2, 0))
                .Should().BeEquivalentTo(
                    new[] { new Hex(1, 0), new Hex(2, 0), new Hex(2, -1), new Hex(1, 1) },
                    o => o.ComparingByValue<Hex>());
        }

        [Test]
        public void StackedPatterns_DeduplicateOverlap()
        {
            // Single adds the anchor (3,0); Line also ends on it — the union must not double-count.
            var covered = Cover(DeliveryPattern.Single | DeliveryPattern.Line, new Hex(3, 0));

            covered.Should().OnlyHaveUniqueItems();
            covered.Should().BeEquivalentTo(
                new[] { new Hex(1, 0), new Hex(2, 0), new Hex(3, 0) },
                o => o.ComparingByValue<Hex>());
        }

        [Test]
        public void SingleAndSelf_CoverBothAnchorAndOrigin()
        {
            Cover(DeliveryPattern.Single | DeliveryPattern.Self, new Hex(2, 0))
                .Should().BeEquivalentTo(
                    new[] { new Hex(2, 0), Origin },
                    o => o.ComparingByValue<Hex>());
        }
    }
}

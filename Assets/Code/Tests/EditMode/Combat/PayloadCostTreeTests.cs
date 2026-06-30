using System;
using Code.Data.Enums;
using Code.Runtime.Core.Combat;
using Code.Runtime.Modules.Statistics;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Locks the pure chain→cost-tree mapping (ADR-0006): payloads become a single linear lineage in
    /// propagation order — the first payload is the lineage root, each later payload nests as the previous
    /// node's single child — so the walker pays them in order and an unaffordable upstream node prunes the
    /// rest. Generic in the payload type, so no weapon fakes are needed.
    /// </summary>
    [TestFixture]
    public sealed class PayloadCostTreeTests
    {
        private static Modifier Flat(float v) => new(v, ModifierType.FlatAdd, Guid.NewGuid());

        [Test]
        public void Empty_ProducesNoRoots()
        {
            var (roots, ordered) = PayloadCostTree.BuildLineage(Array.Empty<(Modifier, string)>());

            roots.Should().BeEmpty();
            ordered.Should().BeEmpty();
        }

        [Test]
        public void Single_ProducesOneRootCarryingItsCostAndPayload()
        {
            var cost = Flat(7f);
            var (roots, ordered) = PayloadCostTree.BuildLineage(new[] { (cost, "bomb") });

            roots.Should().HaveCount(1);
            roots[0].Children.Should().BeEmpty();
            ((float)roots[0].Cost).Should().Be(7f);

            ordered.Should().HaveCount(1);
            ordered[0].node.Should().BeSameAs(roots[0]);
            ordered[0].payload.Should().Be("bomb");
        }

        [Test]
        public void Multiple_NestEachAsThePreviousChild_InPropagationOrder()
        {
            var a = Flat(1f);
            var b = Flat(2f);
            var c = Flat(3f);

            var (roots, ordered) = PayloadCostTree.BuildLineage(new[] { (a, "a"), (b, "b"), (c, "c") });

            // One lineage: a is the root, b is a's only child, c is b's only child.
            roots.Should().HaveCount(1);
            var root = roots[0];
            ((float)root.Cost).Should().Be(1f);
            root.Children.Should().HaveCount(1);

            var second = root.Children[0];
            ((float)second.Cost).Should().Be(2f);
            second.Children.Should().HaveCount(1);

            var third = second.Children[0];
            ((float)third.Cost).Should().Be(3f);
            third.Children.Should().BeEmpty();

            // ordered pairs mirror input order and point at the right nodes.
            ordered.Should().HaveCount(3);
            ordered[0].node.Should().BeSameAs(root);
            ordered[1].node.Should().BeSameAs(second);
            ordered[2].node.Should().BeSameAs(third);
            ordered[0].payload.Should().Be("a");
            ordered[1].payload.Should().Be("b");
            ordered[2].payload.Should().Be("c");
        }
    }
}

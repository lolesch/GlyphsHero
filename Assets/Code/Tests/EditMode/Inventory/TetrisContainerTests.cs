using Code.Runtime.Modules.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditMode.Inventory
{
    /// <summary>
    /// Locks the container's ownership of the resolved chain topology (Candidate 2-A):
    /// the topology is resolved once and cached, recomputed only when contents change.
    /// Uses 1x1 fake items, so a placement position maps to exactly one cell.
    /// </summary>
    [TestFixture]
    public sealed class TetrisContainerTests
    {
        private static TetrisContainer NewContainer() => new(new Vector2Int(4, 1));

        [Test]
        public void Topology_ReflectsCurrentContents()
        {
            var container = NewContainer();
            container.Topology.Chains.Should().BeEmpty();

            ITetrisItem weapon = new FakeWeapon("Weapon");
            container.TryAddAt(new Vector2Int(0, 0), ref weapon).Should().BeTrue();

            container.Topology.Chains.Should().HaveCount(1);
            container.Topology.Chains[0].Weapon.Should().BeSameAs(weapon);
        }

        [Test]
        public void Topology_ReturnsSameInstance_WhenContentsUnchanged()
        {
            var container = NewContainer();
            ITetrisItem weapon = new FakeWeapon("Weapon");
            container.TryAddAt(new Vector2Int(0, 0), ref weapon).Should().BeTrue();

            var first  = container.Topology;
            var second = container.Topology;

            // Resolve-once: repeated reads (e.g. tooltip hovers) must not re-resolve.
            first.Should().BeSameAs(second);
        }

        [Test]
        public void Topology_Recomputes_AfterContentsChange()
        {
            var container = NewContainer();
            ITetrisItem first = new FakeWeapon("First");
            container.TryAddAt(new Vector2Int(0, 0), ref first).Should().BeTrue();

            var before = container.Topology;
            before.Chains.Should().HaveCount(1);

            ITetrisItem second = new FakeWeapon("Second");
            container.TryAddAt(new Vector2Int(1, 0), ref second).Should().BeTrue();

            var after = container.Topology;
            after.Should().NotBeSameAs(before);
            after.Chains.Should().HaveCount(2);
        }

        [Test]
        public void Topology_Recomputes_AfterRemoval()
        {
            var container = NewContainer();
            ITetrisItem weapon = new FakeWeapon("Weapon");
            container.TryAddAt(new Vector2Int(0, 0), ref weapon).Should().BeTrue();

            var before = container.Topology;
            before.Chains.Should().HaveCount(1);

            container.TryRemove(new Vector2Int(0, 0), out _).Should().BeTrue();

            var after = container.Topology;
            after.Should().NotBeSameAs(before);
            after.Chains.Should().BeEmpty();
        }

        // --- TrySwapInto: the unified drop/swap rule (Candidate 3) ---
        // Same- and cross-container drops both flow through this one method, so the swap rules are
        // testable in isolation from the drag MonoBehaviour. Names use 1x1 fakes unless a size is given.

        [Test]
        public void TrySwapInto_PlacesIntoEmptyCell_SettlesEmptyHanded()
        {
            var container = new TetrisContainer(new Vector2Int(2, 1));

            ITetrisItem incoming = new FakeWeapon("A");
            var placed = container.TrySwapInto(
                new Vector2Int(0, 0), ref incoming, container, new Vector2Int(0, 0));

            placed.Should().BeTrue();
            incoming.Should().BeNull("an empty target cell needs no swap");
            container.Contents[new Vector2Int(0, 0)].Name.Should().Be("A");
        }

        [Test]
        public void TrySwapInto_SameContainer_ReturnsDisplacedToFreedSourceCell()
        {
            // KNOWN_ISSUES line 35: same-container drag must attempt a full swap, not always force-pickup.
            var container = new TetrisContainer(new Vector2Int(2, 1));
            ITetrisItem a = new FakeWeapon("A");
            ITetrisItem b = new FakeWeapon("B");
            container.TryAddAt(new Vector2Int(0, 0), ref a).Should().BeTrue();
            container.TryAddAt(new Vector2Int(1, 0), ref b).Should().BeTrue();

            container.TryRemove(new Vector2Int(0, 0), out var held).Should().BeTrue(); // pick up A

            ITetrisItem incoming = held;
            var placed = container.TrySwapInto(
                new Vector2Int(1, 0), ref incoming, container, new Vector2Int(0, 0));

            placed.Should().BeTrue();
            incoming.Should().BeNull("B fits the freed source cell, so the swap completes empty-handed");
            container.Contents[new Vector2Int(1, 0)].Name.Should().Be("A");
            container.Contents[new Vector2Int(0, 0)].Name.Should().Be("B");
        }

        [Test]
        public void TrySwapInto_CrossContainer_ReturnsDisplacedToSource()
        {
            var source = new TetrisContainer(new Vector2Int(1, 1));
            var target = new TetrisContainer(new Vector2Int(1, 1));
            ITetrisItem a = new FakeWeapon("A");
            ITetrisItem b = new FakeWeapon("B");
            source.TryAddAt(new Vector2Int(0, 0), ref a).Should().BeTrue();
            target.TryAddAt(new Vector2Int(0, 0), ref b).Should().BeTrue();
            source.TryRemove(new Vector2Int(0, 0), out var held).Should().BeTrue();

            ITetrisItem incoming = held;
            var placed = target.TrySwapInto(
                new Vector2Int(0, 0), ref incoming, source, new Vector2Int(0, 0));

            placed.Should().BeTrue();
            incoming.Should().BeNull();
            target.Contents[new Vector2Int(0, 0)].Name.Should().Be("A");
            source.Contents[new Vector2Int(0, 0)].Name.Should().Be("B");
        }

        [Test]
        public void TrySwapInto_CarriesDisplaced_WhenItDoesNotFitBackAtSource()
        {
            // Source has only one free cell (A's old spot); a 2x1 displaced item cannot fit there.
            var source = new TetrisContainer(new Vector2Int(2, 1));
            ITetrisItem a = new FakeWeapon("A");
            ITetrisItem filler = new FakeWeapon("C");
            source.TryAddAt(new Vector2Int(0, 0), ref a).Should().BeTrue();
            source.TryAddAt(new Vector2Int(1, 0), ref filler).Should().BeTrue();
            source.TryRemove(new Vector2Int(0, 0), out var held).Should().BeTrue();

            var target = new TetrisContainer(new Vector2Int(2, 1));
            ITetrisItem b = new FakeWeapon("B", 2, 1);
            target.TryAddAt(new Vector2Int(0, 0), ref b).Should().BeTrue();

            ITetrisItem incoming = held; // A (1x1)
            var placed = target.TrySwapInto(
                new Vector2Int(0, 0), ref incoming, source, new Vector2Int(0, 0));

            placed.Should().BeTrue();
            incoming.Should().BeSameAs(b, "B cannot fit the single freed source cell, so it is carried");
            target.Contents[new Vector2Int(0, 0)].Name.Should().Be("A");
            source.Contents.Values.Should().ContainSingle().Which.Name.Should().Be("C",
                "the blocked source keeps its filler untouched — no item is silently dropped");
        }

        [Test]
        public void TrySwapInto_Rejected_WhenIncomingDoesNotFit_LeavesEverythingUnchanged()
        {
            var target = new TetrisContainer(new Vector2Int(1, 1));
            ITetrisItem b = new FakeWeapon("B");
            target.TryAddAt(new Vector2Int(0, 0), ref b).Should().BeTrue();
            var source = new TetrisContainer(new Vector2Int(1, 1));

            ITetrisItem incoming = new FakeWeapon("A");
            var placed = target.TrySwapInto(
                new Vector2Int(5, 5), ref incoming, source, new Vector2Int(0, 0)); // out of bounds

            placed.Should().BeFalse();
            incoming.Name.Should().Be("A", "a rejected drop leaves the held item untouched");
            target.Contents[new Vector2Int(0, 0)].Name.Should().Be("B");
        }
    }
}

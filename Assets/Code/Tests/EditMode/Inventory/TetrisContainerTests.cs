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
    }
}

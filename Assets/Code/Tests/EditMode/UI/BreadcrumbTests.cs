using System.Collections.Generic;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the Alt breadcrumb (tooltip-redesign spec §4, slice 6): <see cref="Breadcrumb.Build(IItemChain,ITetrisItem)"/>
    /// renders a chain in <em>real connection order</em> (root → weapon), the hovered item bracketed. This
    /// is the fix for the deleted <c>BuildChainSentence</c>, which drew the path backwards to the grid.
    ///
    /// Red-green: the ordering test pins a string that a reversed/outward walk (the old behaviour) would
    /// get wrong. Mutations that turn these red (a human can confirm in Rider):
    ///  - reversing the item order (root-last) → the ordering test's string flips;
    ///  - dropping the bracket on the hovered item → the bracket tests mismatch;
    ///  - bracketing the wrong item → the wrong token is bracketed;
    ///  - changing the separator → every multi-item string mismatches;
    ///  - not resolving the item's chain from the topology → the topology overload returns empty.
    ///
    /// Fakes: FakeReactor/FakeAmplifier/FakeWeapon carry the given Name; only the Name matters here.
    /// </summary>
    [TestFixture]
    public sealed class BreadcrumbTests
    {
        private static IItemChain Chain(ITetrisItem root, params ITetrisItem[] modifiers) =>
            new ItemChain(root, new List<ITetrisItem>(modifiers));

        // ── Connection order: root → weapon, never outward from the hovered ──

        [Test]
        public void Build_RendersRootToWeapon_InConnectionOrder()
        {
            var reactor = new FakeReactor("Reactor");
            var amp     = new FakeAmplifier("Iron Amp");
            var weapon  = new FakeWeapon("Crossblades");
            var chain   = Chain(reactor, amp, weapon);

            // Hovering the middle amp: the whole path reads root → weapon (a reversed walk would give
            // "Crossblades → [Iron Amp] → Reactor").
            Breadcrumb.Build(chain, amp)
                .Should().Be("Reactor → [Iron Amp] → Crossblades");
        }

        // ── The hovered item is the bracketed one ─────────────────────────

        [Test]
        public void Build_BracketsTheHoveredRoot()
        {
            var reactor = new FakeReactor("Reactor");
            var amp     = new FakeAmplifier("Iron Amp");
            var weapon  = new FakeWeapon("Crossblades");
            var chain   = Chain(reactor, amp, weapon);

            Breadcrumb.Build(chain, reactor)
                .Should().Be("[Reactor] → Iron Amp → Crossblades");
        }

        [Test]
        public void Build_BracketsTheHoveredWeapon()
        {
            var reactor = new FakeReactor("Reactor");
            var amp     = new FakeAmplifier("Iron Amp");
            var weapon  = new FakeWeapon("Crossblades");
            var chain   = Chain(reactor, amp, weapon);

            Breadcrumb.Build(chain, weapon)
                .Should().Be("Reactor → Iron Amp → [Crossblades]");
        }

        [Test]
        public void Build_WeaponRootChain_HoveredWeaponIsSoleBracketedToken()
        {
            var weapon = new FakeWeapon("Crossblades");
            var amp    = new FakeAmplifier("Iron Amp");
            var chain  = Chain(weapon, amp);

            Breadcrumb.Build(chain, weapon)
                .Should().Be("[Crossblades] → Iron Amp");
        }

        // ── Edge cases ────────────────────────────────────────────────────

        [Test]
        public void Build_NullChain_IsEmpty()
        {
            Breadcrumb.Build((IItemChain)null, new FakeWeapon("w"))
                .Should().BeEmpty();
        }

        [Test]
        public void Build_EmptyChain_IsEmpty()
        {
            Breadcrumb.Build(ItemChain.Empty, new FakeWeapon("w"))
                .Should().BeEmpty();
        }

        // ── Topology overload: resolves the item's chain ──────────────────

        [Test]
        public void Build_FromTopology_RendersTheItemsChain()
        {
            var reactor = new FakeReactor("Reactor");
            var amp     = new FakeAmplifier("Iron Amp");
            var weapon  = new FakeWeapon("Crossblades");
            var chain   = Chain(reactor, amp, weapon);

            var container = new FakeStateContainer();
            container.SetChains(chain);

            Breadcrumb.Build(container.Topology, amp)
                .Should().Be("Reactor → [Iron Amp] → Crossblades");
        }

        [Test]
        public void Build_FromTopology_ItemInNoChain_IsEmpty()
        {
            var chain     = Chain(new FakeWeapon("Crossblades"));
            var container = new FakeStateContainer();
            container.SetChains(chain);

            Breadcrumb.Build(container.Topology, new FakeAmplifier("Loose Amp"))
                .Should().BeEmpty();
        }
    }
}

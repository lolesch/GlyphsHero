using System.Collections.Generic;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks ADR-0010's Tier-1/Details gates on <see cref="ItemTooltipController.BuildTooltip"/> (made
    /// <c>internal</c> + <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute"/>'d to
    /// this assembly for exactly this seam — the method is otherwise pure given fakes).
    ///
    /// Red-green: written against the pre-ADR-0010 behavior, where every gated line below rendered
    /// unconditionally regardless of <c>detailed</c> — each <c>_Default_Hides…</c> test would have
    /// failed before its call site grew an <c>if (detailed)</c>.
    ///
    /// Decision 1 (Tier-1 whitelist) and Decision 2 (counterfactual state is Details-only) are two
    /// sides of the same presenter gate, so one pair of tests per call site covers both. Decision 4
    /// (payload cost/timing) shares the same shape.
    /// </summary>
    [TestFixture]
    public sealed class ItemTooltipControllerTests
    {
        private static ChainTopology TopologyOf(params IItemChain[] chains) => new(
            new List<IItemChain>(chains),
            new HashSet<(Vector2Int, Vector2Int)>(),
            new Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>>(),
            new Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>>(),
            new HashSet<ITetrisItem>());

        // ── Decision 2: a chained attachment's dim Unchained "other" state ─

        [Test]
        public void ChainedAttachment_Default_HidesUnchainedOtherState()
        {
            var weapon = new FakeWeapon("Weapon");
            var amp    = new FakeAmplifier("Amp");
            var chain  = new ItemChain(weapon, new List<ITetrisItem> { amp });

            var tooltip = ItemTooltipController.BuildTooltip(amp, TopologyOf(chain),
                detailed: false, isOwned: true, ownerStats: null);

            tooltip.Should().NotContain(StateGlyphs.For(ItemStateKind.Unchained));
        }

        [Test]
        public void ChainedAttachment_Details_ShowsUnchainedOtherState()
        {
            var weapon = new FakeWeapon("Weapon");
            var amp    = new FakeAmplifier("Amp");
            var chain  = new ItemChain(weapon, new List<ITetrisItem> { amp });

            var tooltip = ItemTooltipController.BuildTooltip(amp, TopologyOf(chain),
                detailed: true, isOwned: true, ownerStats: null);

            tooltip.Should().Contain(StateGlyphs.For(ItemStateKind.Unchained));
        }

        // ── Decision 2: a chained driving weapon's dim "as payload" state ──

        [Test]
        public void ChainedDrivingWeapon_Default_HidesAsPayloadOtherState()
        {
            var weapon = new FakeWeapon("Weapon");
            var chain  = new ItemChain(weapon, new List<ITetrisItem>());

            var tooltip = ItemTooltipController.BuildTooltip(weapon, TopologyOf(chain),
                detailed: false, isOwned: true, ownerStats: null);

            tooltip.Should().NotContain(StateGlyphs.For(ItemStateKind.Payload));
        }

        [Test]
        public void ChainedDrivingWeapon_Details_ShowsAsPayloadOtherState()
        {
            var weapon = new FakeWeapon("Weapon");
            var chain  = new ItemChain(weapon, new List<ITetrisItem>());

            var tooltip = ItemTooltipController.BuildTooltip(weapon, TopologyOf(chain),
                detailed: true, isOwned: true, ownerStats: null);

            tooltip.Should().Contain(StateGlyphs.For(ItemStateKind.Payload));
        }

        // ── Decision 2: a standalone (unchained) weapon's dim "as payload" state ──

        [Test]
        public void StandaloneWeapon_Default_HidesAsPayloadOtherState()
        {
            var weapon = new FakeWeapon("Weapon");

            var tooltip = ItemTooltipController.BuildTooltip(weapon, ChainTopology.Empty,
                detailed: false, isOwned: true, ownerStats: null);

            tooltip.Should().NotContain(StateGlyphs.For(ItemStateKind.Payload));
        }

        [Test]
        public void StandaloneWeapon_Details_ShowsAsPayloadOtherState()
        {
            var weapon = new FakeWeapon("Weapon");

            var tooltip = ItemTooltipController.BuildTooltip(weapon, ChainTopology.Empty,
                detailed: true, isOwned: true, ownerStats: null);

            tooltip.Should().Contain(StateGlyphs.For(ItemStateKind.Payload));
        }

        // ── Decision 4: a payload weapon's cost line (FakeWeapon.Payload is always null, so this
        // also covers the "free to add" branch of AppendPayloadOutput's cost line) ──

        [Test]
        public void PayloadWeapon_Default_HidesCostLine()
        {
            var driving = new FakeWeapon("Root");
            var payload = new FakeWeapon("Bolt");
            var chain   = new ItemChain(driving, new List<ITetrisItem> { payload });

            var tooltip = ItemTooltipController.BuildTooltip(payload, TopologyOf(chain),
                detailed: false, isOwned: true, ownerStats: null);

            tooltip.Should().NotContain("free to add");
        }

        [Test]
        public void PayloadWeapon_Details_ShowsCostLine()
        {
            var driving = new FakeWeapon("Root");
            var payload = new FakeWeapon("Bolt");
            var chain   = new ItemChain(driving, new List<ITetrisItem> { payload });

            var tooltip = ItemTooltipController.BuildTooltip(payload, TopologyOf(chain),
                detailed: true, isOwned: true, ownerStats: null);

            tooltip.Should().Contain("free to add");
        }
    }
}

using System.Collections.Generic;
using Code.Data.Enums;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the <b>axis from → to</b> expansion (tooltip-redesign spec §3 Converter row, slice 6):
    /// <see cref="PositionalDelta.AxisDeltas"/> turns a piece's categorical reclassifications
    /// (Delivery / Affinity / Anchor / cost-pool) into lines that name only the <em>result</em>
    /// (<c>→ Aoe</c>) without Alt, and the whole <em>from → to</em> move (<c>Single → Aoe</c>) with Alt.
    ///
    /// Red-green: the detailed/plain pair pins the Alt behaviour — a build that ignores the flag (always
    /// <c>→ Aoe</c>) fails the detailed case; one that always shows from→to fails the plain case. The
    /// additive rule is pinned by the "no change → empty" cases. Mutations a human can confirm in Rider:
    ///  - dropping the <paramref name="detailed"/> branch → detailed cases lose their "from" side;
    ///  - not gating on before != with → an unchanged axis prints a phantom "X → X" line;
    ///  - reading the pool without its "pool" lead → the resource swap reads like an axis conversion.
    ///
    /// Fake defaults leaned on (ChainFakes): FakeWeapon seeds Delivery=Single, Affinity=Hostile,
    /// Anchor=Target, cost pool=Mana. FakeConverter converts to Aoe / Friendly / Origin / Health on the
    /// axis it is built for.
    /// </summary>
    [TestFixture]
    public sealed class AxisDeltaTests
    {
        private static IItemChain Chain(ITetrisItem root, params ITetrisItem[] modifiers) =>
            new ItemChain(root, new List<ITetrisItem>(modifiers));

        // The marginal delta of the single non-weapon piece in a weapon+piece chain.
        private static PieceDelta PieceOf(ITetrisItem piece)
        {
            var pieces = PositionalDelta.Pieces(Chain(new FakeWeapon("w"), piece));
            pieces.Should().ContainSingle();
            return pieces[0];
        }

        // ── Delivery axis (the spec's worked example) ─────────────────────

        [Test]
        public void Delivery_PlainNamesOnlyTheResult()
        {
            var piece = PieceOf(new FakeConverter("c", ConverterAxis.Delivery)); // Single → Aoe

            PositionalDelta.AxisDeltas(piece, detailed: false).Should().Equal("→ Aoe");
        }

        [Test]
        public void Delivery_AltExpandsToFromArrowTo()
        {
            var piece = PieceOf(new FakeConverter("c", ConverterAxis.Delivery));

            PositionalDelta.AxisDeltas(piece, detailed: true).Should().Equal("Single → Aoe");
        }

        // ── Affinity / Anchor axes (same rule, different axis) ────────────

        [Test]
        public void Affinity_AltExpandsFromHostileToFriendly()
        {
            var piece = PieceOf(new FakeConverter("c", ConverterAxis.Affinity)); // Hostile → Friendly

            PositionalDelta.AxisDeltas(piece, detailed: false).Should().Equal("→ Friendly");
            PositionalDelta.AxisDeltas(piece, detailed: true).Should().Equal("Hostile → Friendly");
        }

        [Test]
        public void Anchor_AltExpandsFromTargetToOrigin()
        {
            var piece = PieceOf(new FakeConverter("c", ConverterAxis.Anchor)); // Target → Origin

            PositionalDelta.AxisDeltas(piece, detailed: false).Should().Equal("→ Origin");
            PositionalDelta.AxisDeltas(piece, detailed: true).Should().Equal("Target → Origin");
        }

        // ── Cost pool keeps its "pool" lead ───────────────────────────────

        [Test]
        public void Pool_KeepsPoolLeadAndExpandsUnderAlt()
        {
            var piece = PieceOf(new FakeConverter("c", ConverterAxis.Resource)); // Mana → Health

            PositionalDelta.AxisDeltas(piece, detailed: false).Should().Equal("pool → Health");
            PositionalDelta.AxisDeltas(piece, detailed: true).Should().Equal("pool Mana → Health");
        }

        // ── Additive: a piece that changes no axis contributes nothing ────

        [Test]
        public void NoAxisChange_IsEmpty()
        {
            var piece = PieceOf(new FakeAmplifier("a")); // shapes damage, touches no axis

            PositionalDelta.AxisDeltas(piece, detailed: false).Should().BeEmpty();
            PositionalDelta.AxisDeltas(piece, detailed: true).Should().BeEmpty();
        }
    }
}

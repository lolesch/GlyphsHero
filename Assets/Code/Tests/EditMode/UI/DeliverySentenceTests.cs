using Code.Data.Enums;
using Code.Runtime.UI.Inventory;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the delivery sentence (tooltip-redesign slice 2): <see cref="DeliverySentence.Build"/>
    /// turns Pattern × Affinity × Anchor (+ Aoe radius) into one verb-led, grammatical sentence — never
    /// the old robot output (<c>Single · hits enemies · on target</c>).
    ///
    /// Red-green: each case pins a distinct exact string, so a wrong verb/subject/anchor arm, a dropped
    /// Aoe radius, a broken self-collapse, or a flags-priority slip fails its own case. The four spec
    /// examples are asserted verbatim; the rest fan the remaining axis combinations out.
    /// </summary>
    [TestFixture]
    public sealed class DeliverySentenceTests
    {
        // ── The four locked spec examples (verbatim) ──────────────────────

        [Test]
        public void Single_Enemies_Target() =>
            DeliverySentence.Build(DeliveryPattern.Single, Affinity.Hostile, Anchor.Target, 0)
                .Should().Be("Strikes a single enemy at the target");

        [Test]
        public void Aoe_Enemies_Target_IncludesRadius() =>
            DeliverySentence.Build(DeliveryPattern.Aoe, Affinity.Hostile, Anchor.Target, 2)
                .Should().Be("Blasts enemies within 2 of the target");

        [Test]
        public void Single_Friendly_Origin_CollapsesToBuffSelf() =>
            DeliverySentence.Build(DeliveryPattern.Single, Affinity.Friendly, Anchor.Origin, 0)
                .Should().Be("Buffs self");

        [Test]
        public void Aoe_Friendly_Origin_HealsWithinRadiusOfSelf() =>
            DeliverySentence.Build(DeliveryPattern.Aoe, Affinity.Friendly, Anchor.Origin, 1)
                .Should().Be("Heals allies within 1 of self");

        // ── Pattern coverage (Hostile · Target) ───────────────────────────

        [Test]
        public void Line_Enemies_Target() =>
            DeliverySentence.Build(DeliveryPattern.Line, Affinity.Hostile, Anchor.Target, 0)
                .Should().Be("Pierces enemies in a line to the target");

        [Test]
        public void Cleave_Enemies_Target() =>
            DeliverySentence.Build(DeliveryPattern.Cleave, Affinity.Hostile, Anchor.Target, 0)
                .Should().Be("Cleaves enemies around the target");

        // ── Anchor = Origin (non-collapsing patterns read "self") ─────────

        [Test]
        public void Line_Enemies_Origin_ReadsToSelf() =>
            DeliverySentence.Build(DeliveryPattern.Line, Affinity.Hostile, Anchor.Origin, 0)
                .Should().Be("Pierces enemies in a line to self");

        // ── Friendly verbs ────────────────────────────────────────────────

        [Test]
        public void Single_Friendly_Target_Buffs() =>
            DeliverySentence.Build(DeliveryPattern.Single, Affinity.Friendly, Anchor.Target, 0)
                .Should().Be("Buffs an ally at the target");

        [Test]
        public void Cleave_Friendly_Target_Heals() =>
            DeliverySentence.Build(DeliveryPattern.Cleave, Affinity.Friendly, Anchor.Target, 0)
                .Should().Be("Heals allies around the target");

        // ── Self-collapses ────────────────────────────────────────────────

        // Affinity.Self only ever lands on the caster, whatever the geometry.
        [Test]
        public void SelfAffinity_AlwaysBuffsSelf_RegardlessOfPattern() =>
            DeliverySentence.Build(DeliveryPattern.Aoe, Affinity.Self, Anchor.Target, 3)
                .Should().Be("Buffs self");

        // A Single anchored on the origin covers only the caster's own hex — a deliberate self-hurt.
        [Test]
        public void Single_Hostile_Origin_HurtsSelf() =>
            DeliverySentence.Build(DeliveryPattern.Single, Affinity.Hostile, Anchor.Origin, 0)
                .Should().Be("Hurts self");

        // ── Flags priority (covered hexes are a union; the dominant flag leads) ──

        [Test]
        public void CombinedMask_DescribesMostExpansiveFlag() =>
            DeliverySentence.Build(DeliveryPattern.Single | DeliveryPattern.Aoe, Affinity.Hostile, Anchor.Target, 2)
                .Should().Be("Blasts enemies within 2 of the target");

        // ── Degenerate input ──────────────────────────────────────────────

        [Test]
        public void None_HasNoDelivery() =>
            DeliverySentence.Build(DeliveryPattern.None, Affinity.Hostile, Anchor.Target, 0)
                .Should().Be("Has no delivery");
    }
}

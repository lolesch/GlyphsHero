using System.Collections.Generic;
using Code.Data.Enums;
using Code.Data.Pawns;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;
using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the symmetric two-state model (tooltip-redesign spec §2, slice 5; fixed order per issue #20):
    /// <see cref="TwoStateBlock"/> resolves every item's two states, but their <em>render position</em> is
    /// fixed regardless of which is live — <see cref="TwoStateView.Default"/> is always
    /// Unchained/Driving, <see cref="TwoStateView.Secondary"/> is always Chained/Payload. Only
    /// <see cref="ItemStateView.IsActive"/> tracks the <c>primaryActive</c> flag the presenter passes
    /// (<c>isChained</c> for attachments, <c>!isPayload</c> for weapons).
    ///
    /// Red-green: before issue #20's fix, <c>Build</c> swapped which <em>slot</em> (not just which
    /// <c>IsActive</c> flag) each state landed in based on <c>primaryActive</c> — i.e.
    /// <c>Build(item, primaryActive: true).Default.Kind</c> would be <c>Chained</c>, while
    /// <c>Build(item, primaryActive: false).Default.Kind</c> would be <c>Unchained</c>. The
    /// <c>*_DefaultSlotNeverMoves</c> tests below pin <c>Default.Kind</c>/<c>Secondary.Kind</c> as
    /// constant across both values of <c>primaryActive</c> — reverting to the old swap-by-slot logic turns
    /// them red immediately (a human can confirm in Rider by re-introducing the ternary swap).
    ///
    /// Fake defaults leaned on (ChainFakes): FakeWeapon Damage = 1, Single/Hostile/Target, no Payload;
    /// FakeAmplifier outputMod = Damage +1; FakeReactor = OnSelfHit + AttackSpeed +1; FakeDualAmplifier =
    /// Damage +2 (chained) / LifeMax +5 (loose affix).
    /// </summary>
    [TestFixture]
    public sealed class TwoStateBlockTests
    {
        // ── Fixed render position: Default/Secondary never swap slots ──────

        [Test]
        public void Weapon_DefaultSlotNeverMoves_DrivingFirstPayloadSecond(
            [Values(true, false)] bool primaryActive)
        {
            var block = TwoStateBlock.Build(new FakeWeapon("w"), primaryActive);

            block.Default.Kind.Should().Be(ItemStateKind.Driving);
            block.Secondary.Kind.Should().Be(ItemStateKind.Payload);
        }

        [Test]
        public void Attachment_DefaultSlotNeverMoves_UnchainedFirstChainedSecond(
            [Values(true, false)] bool primaryActive)
        {
            var block = TwoStateBlock.Build(new FakeAmplifier("a"), primaryActive);

            block.Default.Kind.Should().Be(ItemStateKind.Unchained);
            block.Secondary.Kind.Should().Be(ItemStateKind.Chained);
        }

        // ── IsActive (not slot position) tracks primaryActive ──────────────

        [Test]
        public void Weapon_PrimaryActive_DrivingIsActive_PayloadIsOther()
        {
            var block = TwoStateBlock.Build(new FakeWeapon("w"), primaryActive: true);

            block.Default.IsActive.Should().BeTrue();   // Driving
            block.Secondary.IsActive.Should().BeFalse(); // Payload
            block.Active.Kind.Should().Be(ItemStateKind.Driving);
            block.Other.Kind.Should().Be(ItemStateKind.Payload);
        }

        [Test]
        public void Weapon_NotPrimary_PayloadIsActive_DrivingIsOther()
        {
            var block = TwoStateBlock.Build(new FakeWeapon("w"), primaryActive: false);

            block.Default.IsActive.Should().BeFalse();  // Driving, now dim
            block.Secondary.IsActive.Should().BeTrue(); // Payload, now live
            block.Active.Kind.Should().Be(ItemStateKind.Payload);
            block.Other.Kind.Should().Be(ItemStateKind.Driving);
        }

        [Test]
        public void Weapon_DrivingLines_AreDamageThenDeliverySentence()
        {
            var block = TwoStateBlock.Build(new FakeWeapon("w"), primaryActive: true);

            block.Default.Lines.Should().Equal("1.0 dmg", "Strikes a single enemy at the target");
        }

        [Test]
        public void Amplifier_Chained_ChainedActive_ShowsOutputMod()
        {
            var block = TwoStateBlock.Build(new FakeAmplifier("a"), primaryActive: true);

            block.Secondary.IsActive.Should().BeTrue(); // Chained
            block.Secondary.Lines.Should().Equal("Damage +1"); // PositionalDelta.Describe(amp)
            block.Default.IsActive.Should().BeFalse();  // Unchained, dim
        }

        [Test]
        public void Amplifier_Chained_Detailed_ChainedLineResolvesBaseToResult()
        {
            // Issue #29 / ADR-0010 Decision 3: Build threads chain+detailed through to
            // PositionalDelta.Describe, so the Chained line expands to base → result instead of the
            // compact "+50 %" once Details mode is on.
            var weapon = new FakeWeapon("w"); // Damage = 1
            var amp    = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Percent(50f)));
            var chain  = new ItemChain(weapon, new List<ITetrisItem> { amp });

            var block = TwoStateBlock.Build(amp, primaryActive: true, chain: chain, detailed: true);

            block.Secondary.Lines.Should().Equal("Damage 1.0 → 1.5");
        }

        [Test]
        public void Amplifier_Standalone_UnchainedActive()
        {
            var block = TwoStateBlock.Build(new FakeAmplifier("a"), primaryActive: false);

            block.Default.IsActive.Should().BeTrue();    // Unchained
            block.Secondary.IsActive.Should().BeFalse(); // Chained, dim
        }

        [Test]
        public void Reactor_Chained_ShowsFiringConditionThenInputDelta()
        {
            var block = TwoStateBlock.Build(new FakeReactor("r"), primaryActive: true);

            block.Secondary.Kind.Should().Be(ItemStateKind.Chained);
            block.Secondary.Lines.Should().Equal("fires when hit", "AttackSpeed +1");
        }

        // ── The loose affix is the unchained state's content ──────────────

        [Test]
        public void DualAmplifier_ChainedShowsOutput_UnchainedShowsAffix()
        {
            // FakeDualAmplifier is both an output amplifier (Damage +2) and a loose affix (LifeMax +5).
            var block = TwoStateBlock.Build(new FakeDualAmplifier("d"), primaryActive: true);

            block.Secondary.Kind.Should().Be(ItemStateKind.Chained);
            block.Secondary.Lines.Should().Equal("Damage +2");
            block.Default.Kind.Should().Be(ItemStateKind.Unchained);
            block.Default.Lines.Should().Equal("LifeMax +5");
        }

        [Test]
        public void Amplifier_WithoutAffix_UnchainedStateIsEmpty()
        {
            // A plain FakeAmplifier is not an IAttachmentItem, so it carries no loose affix.
            var block = TwoStateBlock.Build(new FakeAmplifier("a"), primaryActive: true);

            block.Default.Kind.Should().Be(ItemStateKind.Unchained);
            block.Default.Lines.Should().BeEmpty();
        }

        // ── Ownerless (stash) vs owned-pawn context for the unchained affix line (issue #22) ──

        private static PawnStats NewPawnStats() => new(ScriptableObject.CreateInstance<PawnConfig>());

        [Test]
        public void Ownerless_Default_AffixLineStaysFlat()
        {
            var block = TwoStateBlock.Build(new FakeDualAmplifier("d"), primaryActive: false,
                detailed: false, isOwned: false);

            block.Default.Lines.Should().Equal("LifeMax +5");
        }

        [Test]
        public void Ownerless_Details_AffixLineAddsDescriptionButNeverAnArrow()
        {
            var block = TwoStateBlock.Build(new FakeDualAmplifier("d"), primaryActive: false,
                detailed: true, isOwned: false);

            block.Default.Lines.Should().Equal("LifeMax +5 — increases max health");
        }

        [Test]
        public void Owned_Details_NoStatsWired_FallsBackFlat()
        {
            // Owned context, but the caller (e.g. no live pawn wired yet) passed no stats — never guess.
            var block = TwoStateBlock.Build(new FakeDualAmplifier("d"), primaryActive: false,
                detailed: true, isOwned: true, stats: null);

            block.Default.Lines.Should().Equal("LifeMax +5");
        }

        [Test]
        public void Owned_Default_StaysFlatEvenWithStatsAvailable()
        {
            // Details mode is the gate, not stats availability — default mode never expands.
            var block = TwoStateBlock.Build(new FakeDualAmplifier("d"), primaryActive: false,
                detailed: false, isOwned: true, stats: NewPawnStats());

            block.Default.Lines.Should().Equal("LifeMax +5");
        }

        [Test]
        public void Owned_Details_WithStats_ResolvesRealBeforeAfterMath()
        {
            // PawnConfig's default baseHealth is 100; FakeDualAmplifier's affix is LifeMax +5.
            var block = TwoStateBlock.Build(new FakeDualAmplifier("d"), primaryActive: false,
                detailed: true, isOwned: true, stats: NewPawnStats());

            block.Default.Lines.Should().Equal("LifeMax 100 → 105");
        }

        // ── Glyph labels replace the old plain-text state labels ───────────

        [Test]
        public void EachState_LabelIsItsGlyph_NotPlainText()
        {
            var block = TwoStateBlock.Build(new FakeWeapon("w"), primaryActive: true);

            block.Default.Label.Should().Be(StateGlyphs.For(ItemStateKind.Driving));
            block.Secondary.Label.Should().Be(StateGlyphs.For(ItemStateKind.Payload));
        }
    }
}

using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the symmetric two-state model (tooltip-redesign spec §2, slice 5): <see cref="TwoStateBlock"/>
    /// resolves every item's two states and <em>which is active</em> — a weapon's driving vs payload role,
    /// an attachment's chained delta vs its loose unchained affix — from the <c>primaryActive</c> flag the
    /// presenter passes (<c>isChained</c> for attachments, <c>!isPayload</c> for weapons).
    ///
    /// Red-green: each case pins the Active/Other <see cref="ItemStateKind"/> arrangement and, where the
    /// fakes make it deterministic, the exact content lines. Mutations that turn these red (a human can
    /// confirm in Rider):
    ///  - swapping the active/other order (ignoring <c>primaryActive</c>) → the Kind assertions flip;
    ///  - sourcing the chained lines from something other than <see cref="PositionalDelta.Describe"/> →
    ///    the amplifier/reactor content mismatches;
    ///  - reading the wrong side of the affix (or dropping it) → the unchained lines mismatch;
    ///  - building the driving line off the wrong axes → the delivery sentence changes.
    ///
    /// Fake defaults leaned on (ChainFakes): FakeWeapon Damage = 1, Single/Hostile/Target, no Payload;
    /// FakeAmplifier outputMod = Damage +1; FakeReactor = OnSelfHit + AttackSpeed +1; FakeDualAmplifier =
    /// Damage +2 (chained) / LifeMax +5 (loose affix).
    /// </summary>
    [TestFixture]
    public sealed class TwoStateBlockTests
    {
        // ── Weapon: driving ⇄ payload, active side follows primaryActive ───

        [Test]
        public void Weapon_PrimaryActive_DrivingIsActive_PayloadIsOther()
        {
            var block = TwoStateBlock.Build(new FakeWeapon("w"), primaryActive: true);

            block.Active.Kind.Should().Be(ItemStateKind.Driving);
            block.Other.Kind.Should().Be(ItemStateKind.Payload);
        }

        [Test]
        public void Weapon_NotPrimary_PayloadIsActive_DrivingIsOther()
        {
            var block = TwoStateBlock.Build(new FakeWeapon("w"), primaryActive: false);

            block.Active.Kind.Should().Be(ItemStateKind.Payload);
            block.Other.Kind.Should().Be(ItemStateKind.Driving);
        }

        [Test]
        public void Weapon_DrivingLines_AreDamageThenDeliverySentence()
        {
            var block = TwoStateBlock.Build(new FakeWeapon("w"), primaryActive: true);

            block.Active.Lines.Should().Equal("1.0 dmg", "Strikes a single enemy at the target");
        }

        // ── Attachment: chained ⇄ unchained, active side follows primaryActive ──

        [Test]
        public void Amplifier_Chained_ChainedActive_ShowsOutputMod()
        {
            var block = TwoStateBlock.Build(new FakeAmplifier("a"), primaryActive: true);

            block.Active.Kind.Should().Be(ItemStateKind.Chained);
            block.Active.Lines.Should().Equal("Damage +1"); // PositionalDelta.Describe(amp)
            block.Other.Kind.Should().Be(ItemStateKind.Unchained);
        }

        [Test]
        public void Amplifier_Standalone_UnchainedActive()
        {
            var block = TwoStateBlock.Build(new FakeAmplifier("a"), primaryActive: false);

            block.Active.Kind.Should().Be(ItemStateKind.Unchained);
            block.Other.Kind.Should().Be(ItemStateKind.Chained);
        }

        [Test]
        public void Reactor_Chained_ShowsFiringConditionThenInputDelta()
        {
            var block = TwoStateBlock.Build(new FakeReactor("r"), primaryActive: true);

            block.Active.Kind.Should().Be(ItemStateKind.Chained);
            block.Active.Lines.Should().Equal("fires when hit", "AttackSpeed +1");
        }

        // ── The loose affix is the unchained state's content ──────────────

        [Test]
        public void DualAmplifier_ChainedShowsOutput_UnchainedShowsAffix()
        {
            // FakeDualAmplifier is both an output amplifier (Damage +2) and a loose affix (LifeMax +5).
            var block = TwoStateBlock.Build(new FakeDualAmplifier("d"), primaryActive: true);

            block.Active.Kind.Should().Be(ItemStateKind.Chained);
            block.Active.Lines.Should().Equal("Damage +2");
            block.Other.Kind.Should().Be(ItemStateKind.Unchained);
            block.Other.Lines.Should().Equal("LifeMax +5");
        }

        [Test]
        public void Amplifier_WithoutAffix_UnchainedStateIsEmpty()
        {
            // A plain FakeAmplifier is not an IAttachmentItem, so it carries no loose affix.
            var block = TwoStateBlock.Build(new FakeAmplifier("a"), primaryActive: true);

            block.Other.Kind.Should().Be(ItemStateKind.Unchained);
            block.Other.Lines.Should().BeEmpty();
        }
    }
}

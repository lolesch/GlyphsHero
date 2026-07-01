using System;
using System.Linq;
using Code.Data.Enums;
using Code.Runtime.Modules.Statistics;
using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the drag-to-compare body (tooltip-redesign spec "Interaction", slice 8): <see cref="CompareBlock"/>
    /// reduces two items to their keyed standalone stats and aligns them so the presenter can print
    /// <c>label held vs slot</c>. <b>Matched</b> keys pair into one two-sided row; a key on only one item
    /// (mismatched types) leaves the other side null.
    ///
    /// Red-green — mutations that turn these red (a human can confirm in Rider):
    ///  - aligning by position instead of key (or reading held for both sides) → the matched-value and
    ///    mismatched-null assertions flip;
    ///  - dropping the pool / changing the F1 or interval format → the weapon value strings mismatch;
    ///  - keying a weapon's absolute dmg the same as an amplifier's +delta → the weapon-vs-amp case
    ///    collapses to a matched row instead of two single-sided rows.
    ///
    /// Fake defaults (ChainFakes): FakeWeapon Damage = 1, AttackSpeed = 1, ResourceCost = 0 [Mana];
    /// FakeAmplifier outputMod = Damage +1; FakeReactor = OnSelfHit + AttackSpeed +1.
    /// </summary>
    [TestFixture]
    public sealed class CompareBlockTests
    {
        // ── Matched: two weapons align dmg / rate / cost, both sides filled ──

        [Test]
        public void TwoWeapons_AlignDmgRateCost_InOrder()
        {
            var view = CompareBlock.Build(new FakeWeapon("held"), new FakeWeapon("slot"));

            view.Rows.Select(r => r.Label).Should().Equal("dmg", "rate", "cost");
            view.Rows.Should().OnlyContain(r => r.Held != null && r.Slot != null); // all matched
        }

        [Test]
        public void TwoWeapons_ReadEachSideIndependently()
        {
            var strong = new FakeWeapon("strong");
            strong.Damage.AddModifier(new Modifier(11f, ModifierType.FlatAdd, Guid.NewGuid())); // 1 → 12

            var view = CompareBlock.Build(new FakeWeapon("weak"), strong);

            var dmg = view.Rows.Single(r => r.Label == "dmg");
            dmg.Held.Should().Be("1.0");   // held weapon's own damage
            dmg.Slot.Should().Be("12.0");  // slot weapon's own damage — not the held value
        }

        [Test]
        public void Weapon_Rate_IsIntervalSeconds_Cost_CarriesPool()
        {
            var view = CompareBlock.Build(new FakeWeapon("held"), new FakeWeapon("slot"));

            view.Rows.Single(r => r.Label == "rate").Held.Should().Be("1.00s"); // 1 / AttackSpeed
            view.Rows.Single(r => r.Label == "cost").Held.Should().Be("0.0 [Mana]");
        }

        // ── Matched: two same-stat amplifiers pair into one row ─────────────

        [Test]
        public void TwoAmplifiers_SameStat_ProduceOneMatchedRow()
        {
            var view = CompareBlock.Build(new FakeAmplifier("a"), new FakeAmplifier("b"));

            view.Rows.Should().ContainSingle();
            var row = view.Rows.Single();
            row.Label.Should().Be("Damage");
            row.Held.Should().Be("+1");
            row.Slot.Should().Be("+1");
        }

        // ── Mismatched types: no shared keys → single-sided rows ────────────

        [Test]
        public void WeaponVsAmplifier_ShareNoKeys_EachRowIsSingleSided()
        {
            var view = CompareBlock.Build(new FakeWeapon("w"), new FakeAmplifier("a"));

            // The weapon's three stats have no slot side; the amplifier's Damage has no held side.
            view.Rows.Where(r => r.Label is "dmg" or "rate" or "cost")
                .Should().OnlyContain(r => r.Held != null && r.Slot == null);
            var ampRow = view.Rows.Single(r => r.Label == "Damage");
            ampRow.Held.Should().BeNull();
            ampRow.Slot.Should().Be("+1");
        }

        // ── Reactor: the firing condition is a compared key ─────────────────

        [Test]
        public void Reactors_CompareFiringConditionAndInputMod()
        {
            var held = new FakeReactor("held", ReactorType.OnManaDeplete);
            var slot = new FakeReactor("slot", ReactorType.OnSelfHit);

            var view = CompareBlock.Build(held, slot);

            var fires = view.Rows.Single(r => r.Label == "fires");
            fires.Held.Should().Be("when mana empties");
            fires.Slot.Should().Be("when hit");
            // The reactor's meaningful input modifier is an additive compared row.
            view.Rows.Single(r => r.Label == "AttackSpeed").Slot.Should().Be("+1");
        }

        // ── Names + null tolerance ──────────────────────────────────────────

        [Test]
        public void View_CarriesBothItemNames()
        {
            var view = CompareBlock.Build(new FakeWeapon("Iron Sword"), new FakeWeapon("Steel Sword"));

            view.HeldName.Should().Be("Iron Sword");
            view.SlotName.Should().Be("Steel Sword");
        }

        [Test]
        public void NullSlot_LeavesEveryHeldRowSingleSided()
        {
            // The wiring only invokes Build for an occupied slot; Build still tolerates a null side by
            // contributing no stats for it, so a stray call degrades to a held-only read rather than throwing.
            var view = CompareBlock.Build(new FakeWeapon("held"), null);

            view.Rows.Should().NotBeEmpty();
            view.Rows.Should().OnlyContain(r => r.Slot == null);
        }
    }
}

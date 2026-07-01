using System;
using Code.Data.Enums;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;
using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the per-attachment <b>active-delta content</b> (tooltip-redesign spec §3, slice 4):
    /// <see cref="PositionalDelta.Describe"/> reads each attachment type's own modifiers/axis into the
    /// §3 "active delta" lines — amplifier output, reactor condition + input, shifter input↔output move,
    /// converter → target — <b>additively</b> (a numeric line only when its modifier is non-default).
    ///
    /// Red-green: each case pins the exact line(s). Mutations that turn these red (a human can confirm
    /// in Rider):
    ///  - dropping the reactor's input line → the reactor case loses its second entry;
    ///  - not filtering no-op modifiers → the zero-mod cases gain a phantom line;
    ///  - rendering the converter's <em>from</em> side, or the wrong <c>To*</c> → the converter cases
    ///    print the wrong target;
    ///  - a wrong <see cref="PositionalDelta.FiringCondition"/> arm → the condition text mismatches.
    ///
    /// Fake defaults leaned on (ChainFakes): FakeAmplifier outputMod = Damage +1 (flat), FakeReactor =
    /// OnSelfHit + inputMod AttackSpeed +1 (flat), FakeShifter = AttackSpeed +1 ↔ Damage +1, FakeConverter
    /// = Delivery → Aoe. Modifier.ToString renders flat +1 as "+1" and PercentAdd 20 as "+20 %".
    /// </summary>
    [TestFixture]
    public sealed class AttachmentDeltaTests
    {
        // ── Amplifier: output modifier line ───────────────────────────────

        [Test]
        public void Amplifier_ShowsItsOutputModifier()
        {
            PositionalDelta.Describe(new FakeAmplifier("a"))
                .Should().Equal("Damage +1");
        }

        [Test]
        public void Amplifier_RendersPercentOutput()
        {
            var amp = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Percent(20f)));

            PositionalDelta.Describe(amp).Should().Equal("Damage +20 %");
        }

        [Test]
        public void Amplifier_NoOpModifier_IsAdditivelyDropped()
        {
            var amp = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Flat(0f)));

            PositionalDelta.Describe(amp).Should().BeEmpty();
        }

        [Test]
        public void Amplifier_PercentMultAlwaysShows_EvenAtZero()
        {
            // A ×0 % multiplier zeroes damage — a deliberate authored value, never a no-op line to hide.
            var mult = new WeaponOutputModifier(WeaponOutputStat.Damage,
                new Modifier(0f, ModifierType.PercentMult, Guid.NewGuid()));

            PositionalDelta.Describe(new StatAmplifier(mult)).Should().ContainSingle();
        }

        // ── Reactor: firing condition + input delta ───────────────────────

        [Test]
        public void Reactor_ShowsFiringConditionThenInputDelta()
        {
            PositionalDelta.Describe(new FakeReactor("r"))
                .Should().Equal("fires when hit", "AttackSpeed +1");
        }

        [Test]
        public void Reactor_NoOpInput_ShowsOnlyTheFiringCondition()
        {
            var reactor = new StatReactor(Mods.Input(WeaponInputStat.AttackSpeed, Mods.Flat(0f)),
                ReactorType.OnManaDeplete);

            PositionalDelta.Describe(reactor).Should().Equal("fires when mana empties");
        }

        // ── Shifter: input↔output economy trade ───────────────────────────

        [Test]
        public void Shifter_ShowsInputToOutputMove()
        {
            PositionalDelta.Describe(new FakeShifter("s"))
                .Should().Equal("AttackSpeed +1 ↔ Damage +1");
        }

        // ── Converter: converts-to target on its axis ─────────────────────

        [Test]
        public void Converter_ShowsDeliveryTarget()
        {
            PositionalDelta.Describe(new FakeConverter("c")) // Delivery → Aoe
                .Should().Equal("→ Aoe");
        }

        [Test]
        public void Converter_ShowsResourceTargetOnResourceAxis()
        {
            var converter = new StatConverter(ConverterAxis.Resource, toResource: ResourceType.Health);

            PositionalDelta.Describe(converter).Should().Equal("→ Health");
        }

        [Test]
        public void Converter_ShowsAffinityTargetOnAffinityAxis()
        {
            var converter = new StatConverter(ConverterAxis.Affinity, toAffinity: Affinity.Friendly);

            PositionalDelta.Describe(converter).Should().Equal("→ Friendly");
        }

        // ── Non-attachments carry no active-delta content ─────────────────

        [Test]
        public void Weapon_HasNoAttachmentContent()
        {
            PositionalDelta.Describe(new FakeWeapon("w")).Should().BeEmpty();
        }

        // ── Firing-condition map ──────────────────────────────────────────

        [Test]
        public void FiringCondition_MapsKnownReactorTypes()
        {
            PositionalDelta.FiringCondition(ReactorType.OnSelfHit).Should().Be("when hit");
            PositionalDelta.FiringCondition(ReactorType.OnEnemyDeath).Should().Be("when an enemy dies");
            PositionalDelta.FiringCondition(ReactorType.OnNearbyEnemyDies)
                .Should().Be("when a nearby enemy dies");
        }
    }
}

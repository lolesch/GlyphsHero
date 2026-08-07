using System;
using System.Collections.Generic;
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
            PositionalDelta.Describe(new FakeAmplifier("a"), chain: null, detailed: false)
                .Should().Equal("Damage +1");
        }

        [Test]
        public void Amplifier_RendersPercentOutput()
        {
            var amp = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Percent(20f)));

            PositionalDelta.Describe(amp, chain: null, detailed: false).Should().Equal("Damage +20 %");
        }

        [Test]
        public void Amplifier_NoOpModifier_IsAdditivelyDropped()
        {
            var amp = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Flat(0f)));

            PositionalDelta.Describe(amp, chain: null, detailed: false).Should().BeEmpty();
        }

        [Test]
        public void Amplifier_PercentMultAlwaysShows_EvenAtZero()
        {
            // A ×0 % multiplier zeroes damage — a deliberate authored value, never a no-op line to hide.
            var mult = new WeaponOutputModifier(WeaponOutputStat.Damage,
                new Modifier(0f, ModifierType.PercentMult, Guid.NewGuid()));

            PositionalDelta.Describe(new StatAmplifier(mult), chain: null, detailed: false).Should().ContainSingle();
        }

        // ── Reactor: firing condition + input delta ───────────────────────

        [Test]
        public void Reactor_ShowsFiringConditionThenInputDelta()
        {
            PositionalDelta.Describe(new FakeReactor("r"), chain: null, detailed: false)
                .Should().Equal("fires when hit", "AttackSpeed +1");
        }

        [Test]
        public void Reactor_NoOpInput_ShowsOnlyTheFiringCondition()
        {
            var reactor = new StatReactor(Mods.Input(WeaponInputStat.AttackSpeed, Mods.Flat(0f)),
                ReactorType.OnManaDeplete);

            PositionalDelta.Describe(reactor, chain: null, detailed: false).Should().Equal("fires when mana empties");
        }

        // ── Shifter: input↔output economy trade ───────────────────────────

        [Test]
        public void Shifter_ShowsInputToOutputMove()
        {
            PositionalDelta.Describe(new FakeShifter("s"), chain: null, detailed: false)
                .Should().Equal("AttackSpeed +1 ↔ Damage +1");
        }

        // ── Converter: converts-to target on its axis ─────────────────────

        [Test]
        public void Converter_ShowsDeliveryTarget()
        {
            PositionalDelta.Describe(new FakeConverter("c"), chain: null, detailed: false) // Delivery → Aoe
                .Should().Equal("→ Aoe");
        }

        [Test]
        public void Converter_ShowsResourceTargetOnResourceAxis()
        {
            var converter = new StatConverter(ConverterAxis.Resource, toResource: ResourceType.Health);

            PositionalDelta.Describe(converter, chain: null, detailed: false).Should().Equal("→ Health");
        }

        [Test]
        public void Converter_ShowsAffinityTargetOnAffinityAxis()
        {
            var converter = new StatConverter(ConverterAxis.Affinity, toAffinity: Affinity.Friendly);

            PositionalDelta.Describe(converter, chain: null, detailed: false).Should().Equal("→ Friendly");
        }

        // ── Details mode (issue #29 / ADR-0010 Decision 3): base → result ──
        //
        // Root cause of #29: Describe() printed Modifier.ToString() regardless of Details mode, so a
        // PercentAdd amplifier read "+ 50 %" even under Details, disagreeing with the piece list's own
        // "base → result" numbers for the identical modifier. These pin the fix: given a chain that
        // resolves this item's own before/with, Details mode now reuses those exact numbers; without a
        // chain (a loose item, or Details off) it still falls back to the compact modifier form.

        [Test]
        public void Amplifier_Detailed_WithChain_ResolvesBaseToResult()
        {
            var weapon = new FakeWeapon("w"); // Damage = 1
            var amp    = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Percent(50f)));
            var chain  = new ItemChain(weapon, new List<ITetrisItem> { amp });

            // base 1 * 1.5 = 1.5 — the same number PositionalDelta.Pieces already resolves for this amp.
            PositionalDelta.Describe(amp, chain, detailed: true).Should().Equal("Damage 1.0 → 1.5");
        }

        [Test]
        public void Amplifier_Detailed_WithoutChain_FallsBackToCompactForm()
        {
            var amp = new FakeAmplifier("a"); // outputMod Damage +1 (flat)

            // No chain to resolve a position in → nothing to diff, so Details mode can't expand it.
            PositionalDelta.Describe(amp, chain: null, detailed: true).Should().Equal("Damage +1");
        }

        [Test]
        public void Reactor_Detailed_WithChain_EquationMatchesPieceList()
        {
            var reactor = new FakeReactor("r");        // inputMod AttackSpeed +1 (flat)
            var weapon  = new FakeWeapon("w");          // AttackSpeed = 1
            var chain   = new ItemChain(reactor, new List<ITetrisItem> { weapon });

            // Reuses PositionalDelta.ReactorInputEquation — the exact formatter the piece list calls.
            PositionalDelta.Describe(reactor, chain, detailed: true)
                .Should().Equal("fires when hit", "[base 1] +1 = 2");
        }

        [Test]
        public void Shifter_Detailed_WithChain_ExpandsBothSides()
        {
            var shifter = new FakeShifter("s"); // AttackSpeed +1 ↔ Damage +1 (flat)
            var weapon  = new FakeWeapon("w");   // AttackSpeed = 1, Damage = 1
            var chain   = new ItemChain(shifter, new List<ITetrisItem> { weapon });

            PositionalDelta.Describe(shifter, chain, detailed: true)
                .Should().Equal("AttackSpeed 1.0 → 2.0 ↔ Damage 1.0 → 2.0");
        }

        // ── Non-attachments carry no active-delta content ─────────────────

        [Test]
        public void Weapon_HasNoAttachmentContent()
        {
            PositionalDelta.Describe(new FakeWeapon("w"), chain: null, detailed: false).Should().BeEmpty();
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

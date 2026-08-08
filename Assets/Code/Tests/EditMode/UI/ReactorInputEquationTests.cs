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
    /// Locks the reactor's own <b>input</b> equation (tooltip-redesign spec §2.1 / §3 Reactor row,
    /// slice 6 remainder; glyph-wired per v2 slice 8, issue #24):
    /// <see cref="PositionalDelta.ReactorInputEquation"/> is the glyph + modifier alone without Alt, or
    /// the glyph + full <c>[base X] modifier = result</c> equation + the stat's own name with Alt — read
    /// off whichever <see cref="Code.Runtime.Modules.Inventory.WeaponStats"/> field the reactor's
    /// <c>inputMod</c> targets, from the piece's own before/with snapshot (its marginal contribution, not
    /// the whole chain's).
    ///
    /// Red-green: a build that always/never expands under Alt, reads the wrong before/after field, or
    /// forgets <c>ProcChance</c> has no backing field fails one of the cases below.
    /// </summary>
    [TestFixture]
    public sealed class ReactorInputEquationTests
    {
        private static IItemChain Chain(ITetrisItem root, params ITetrisItem[] modifiers) =>
            new ItemChain(root, new List<ITetrisItem>(modifiers));

        [Test]
        public void Plain_ShowsOnlyTheModifierLabel()
        {
            var reactor = new FakeReactor("r"); // inputMod AttackSpeed +1 (flat)
            var chain   = Chain(reactor, new FakeWeapon("w"));
            var piece   = PositionalDelta.Pieces(chain)[0];

            PositionalDelta.ReactorInputEquation(reactor, piece, detailed: false)
                .Should().Be($"{StatGlyphs.For(StatKind.AttackSpeed)} +1");
        }

        [Test]
        public void Alt_AttackSpeed_ShowsBaseModifierEqualsResult()
        {
            var reactor = new FakeReactor("r"); // inputMod AttackSpeed +1 (flat), base AttackSpeed 1
            var chain   = Chain(reactor, new FakeWeapon("w"));
            var piece   = PositionalDelta.Pieces(chain)[0];

            PositionalDelta.ReactorInputEquation(reactor, piece, detailed: true)
                .Should().Be($"{StatGlyphs.For(StatKind.AttackSpeed)} [base 1] +1 = 2 AttackSpeed");
        }

        [Test]
        public void Alt_ManaCost_ReadsResourceCostBeforeAndAfter()
        {
            var reactor = new StatReactor(Mods.Input(WeaponInputStat.ManaCost, Mods.Flat(5f)));
            var weapon  = new StatWeapon(resourceCost: 10f);
            var chain   = Chain(reactor, weapon);
            var piece   = PositionalDelta.Pieces(chain)[0];

            PositionalDelta.ReactorInputEquation(reactor, piece, detailed: true)
                .Should().Be($"{StatGlyphs.For(StatKind.Cost)} [base 10] +5 = 15 Cost");
        }

        [Test]
        public void Alt_ProcChance_HasNoBackingField_FallsBackToLabel()
        {
            // ProcChance has no WeaponStats field (WeaponStatResolver drops it silently) — even under
            // Alt there's no before/after to show, so the value stays the modifier alone; it still gains
            // the glyph + Details-mode label since that's driven by the detailed flag, not by resolution.
            var reactor = new StatReactor(Mods.Input(WeaponInputStat.ProcChance, Mods.Flat(10f)));
            var chain   = Chain(reactor, new StatWeapon());
            var piece   = PositionalDelta.Pieces(chain)[0];

            PositionalDelta.ReactorInputEquation(reactor, piece, detailed: true)
                .Should().Be($"{StatGlyphs.For(StatKind.ProcChance)} +10 ProcChance");
        }

        [Test]
        public void NoOpModifier_IsEmpty_EvenUnderAlt()
        {
            var reactor = new StatReactor(Mods.Input(WeaponInputStat.AttackSpeed, Mods.Flat(0f)));
            var chain   = Chain(reactor, new StatWeapon());
            var piece   = PositionalDelta.Pieces(chain)[0];

            PositionalDelta.ReactorInputEquation(reactor, piece, detailed: true).Should().BeEmpty();
            PositionalDelta.ReactorInputEquation(reactor, piece, detailed: false).Should().BeEmpty();
        }
    }
}

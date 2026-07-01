using System.Collections.Generic;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the positional delta model (tooltip-redesign slice 3): <see cref="PositionalDelta.Totals"/>
    /// is the chain's final resolved readout, and <see cref="PositionalDelta.Pieces"/> is the ordered
    /// list of contributing pieces' marginal deltas (root → modifiers, weapons excluded).
    ///
    /// Red-green: each case pins concrete before/with numbers and the exact piece ordering, so a wrong
    /// arm fails its own test. Mutations that turn these red (a human can confirm in Rider):
    ///  - not skipping the weapon in <c>Pieces</c> → an extra entry with a 0-delta weapon;
    ///  - using <c>Take(i)</c>/<c>Take(i+1)</c> off-by-one → shifted before/with values;
    ///  - resolving Totals off the weapon base only (dropping mods) → Totals.Damage stays at base.
    ///
    /// The fakes' defaults are the contract these numbers lean on: FakeWeapon Damage/AttackSpeed = 1,
    /// FakeAmplifier outputMod = Damage +1 (flat), FakeReactor inputMod = AttackSpeed +1 (flat).
    /// </summary>
    [TestFixture]
    public sealed class PositionalDeltaTests
    {
        private static IItemChain Chain(ITetrisItem root, params ITetrisItem[] modifiers) =>
            new ItemChain(root, new List<ITetrisItem>(modifiers));

        // ── Totals: the terminal readout ──────────────────────────────────

        [Test]
        public void Totals_WeaponRoot_FoldsEveryAmplifier()
        {
            var weapon = new FakeWeapon("w");
            var chain  = Chain(weapon, new FakeAmplifier("a1"), new FakeAmplifier("a2"));

            // base 1 + (+1) + (+1) = 3
            PositionalDelta.Totals(chain).Damage.Should().Be(3f);
        }

        [Test]
        public void Totals_NoWeapon_IsDefault()
        {
            var chain = Chain(new FakeAmplifier("a"));

            PositionalDelta.Totals(chain).Damage.Should().Be(0f); // default(WeaponStats)
        }

        // ── Pieces: ordered marginal deltas ───────────────────────────────

        [Test]
        public void Pieces_ExcludesTheDrivingWeapon()
        {
            var weapon = new FakeWeapon("w");
            var chain  = Chain(weapon, new FakeAmplifier("a1"), new FakeAmplifier("a2"));

            var pieces = PositionalDelta.Pieces(chain);

            pieces.Should().HaveCount(2);
            pieces[0].Item.Name.Should().Be("a1");
            pieces[1].Item.Name.Should().Be("a2");
        }

        [Test]
        public void Pieces_EachAmplifierSeesTheChainBeforeIt()
        {
            var weapon = new FakeWeapon("w");
            var chain  = Chain(weapon, new FakeAmplifier("a1"), new FakeAmplifier("a2"));

            var pieces = PositionalDelta.Pieces(chain);

            // a1: 1 → 2 ; a2: 2 → 3 (marginal, in apply order)
            pieces[0].Before.Damage.Should().Be(1f);
            pieces[0].With.Damage.Should().Be(2f);
            pieces[1].Before.Damage.Should().Be(2f);
            pieces[1].With.Damage.Should().Be(3f);
        }

        [Test]
        public void Pieces_ReactorRoot_ReactorIsAPieceWeaponIsNot()
        {
            var reactor = new FakeReactor("r");
            var weapon  = new FakeWeapon("w");
            var chain   = Chain(reactor, weapon, new FakeAmplifier("a"));

            var pieces = PositionalDelta.Pieces(chain);

            pieces.Should().HaveCount(2);         // reactor + amp; weapon excluded
            pieces[0].Item.Should().Be(reactor);
            pieces[1].Item.Name.Should().Be("a");

            // reactor's inputMod is AttackSpeed +1: base 1 → 2, damage untouched
            pieces[0].Before.AttackSpeed.Should().Be(1f);
            pieces[0].With.AttackSpeed.Should().Be(2f);
            pieces[0].With.Damage.Should().Be(1f);
        }

        [Test]
        public void Pieces_PayloadWeaponIsNotListed()
        {
            var weapon  = new FakeWeapon("w");
            var payload = new FakeWeapon("p");
            var chain   = Chain(weapon, new FakeAmplifier("a"), payload);

            var pieces = PositionalDelta.Pieces(chain);

            pieces.Should().ContainSingle().Which.Item.Name.Should().Be("a"); // only the amp
        }

        [Test]
        public void Pieces_NoWeapon_IsEmpty()
        {
            var chain = Chain(new FakeAmplifier("a1"), new FakeAmplifier("a2"));

            PositionalDelta.Pieces(chain).Should().BeEmpty();
        }
    }
}

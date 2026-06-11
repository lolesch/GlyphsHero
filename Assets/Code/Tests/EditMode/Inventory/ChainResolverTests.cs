using System.Linq;
using Code.Runtime.Modules.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditMode.Inventory
{
    /// <summary>
    /// Behavioural lock on ChainResolver — the most complex and bug-prone piece in the
    /// project. Items are 1x1 fakes laid out on a row so neighbours sit at +/-1 on X.
    /// Directions: left = (-1,0), right = (+1,0).
    /// </summary>
    [TestFixture]
    public sealed class ChainResolverTests
    {
        private static readonly Vector2Int Left  = new(-1, 0);
        private static readonly Vector2Int Right = new(1, 0);

        [Test]
        public void LoneWeapon_ProducesOneChainWithThatWeapon()
        {
            var weapon    = new FakeWeapon("Weapon");
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), weapon);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(1);
            chains[0].Weapon.Should().BeSameAs(weapon);
        }

        [Test]
        public void WeaponWithAmplifierOnOneSide_ChainsTheAmplifier()
        {
            // [amp]( -> )[weapon]
            var amp    = new FakeAmplifier("Amp", Right);
            var weapon = new FakeWeapon("Weapon", Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), amp)
                .Place(new Vector2Int(1, 0), weapon);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(1);
            chains[0].Weapon.Should().BeSameAs(weapon);
            chains[0].Modifiers.Should().Contain(amp);
        }

        [Test]
        public void ReactorUpstreamOfWeapon_BecomesTheChainRoot()
        {
            // [reactor]( -> )[weapon]
            var reactor = new FakeReactor("Reactor", Right);
            var weapon  = new FakeWeapon("Weapon", Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), reactor)
                .Place(new Vector2Int(1, 0), weapon);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(1);
            chains[0].Root.Should().BeSameAs(reactor);
            chains[0].Weapon.Should().BeSameAs(weapon);
        }

        [Test]
        public void InvalidConnection_ShifterToAmplifier_IsNotChained()
        {
            // [amp]( -> )[shifter]( <-> )[weapon]
            // Shifter<->Amplifier is an illegal pairing, so the amplifier must be excluded.
            var amp     = new FakeAmplifier("Amp", Right);
            var shifter = new FakeShifter("Shifter", Left, Right);
            var weapon  = new FakeWeapon("Weapon", Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), amp)
                .Place(new Vector2Int(1, 0), shifter)
                .Place(new Vector2Int(2, 0), weapon);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(1);
            chains[0].Modifiers.Should().Contain(weapon);
            chains[0].Modifiers.Should().NotContain(amp);
        }

        [Test]
        public void AmplifierOnBothSides_CurrentlyDuplicatesTheWeaponChain()
        {
            // [ampA]( -> )[weapon]( <- )[ampB]
            // CHARACTERIZATION of the known double-fire bug (KNOWN_ISSUES.md): the weapon
            // appears as the root of TWO chains, which makes it fire twice in combat.
            // When the resolver is made weapon-centric, replace this with the test below.
            var ampA   = new FakeAmplifier("AmpA", Right);
            var weapon = new FakeWeapon("Weapon", Left, Right);
            var ampB   = new FakeAmplifier("AmpB", Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), ampA)
                .Place(new Vector2Int(1, 0), weapon)
                .Place(new Vector2Int(2, 0), ampB);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(2, "the resolver currently emits one chain per root connector");
            chains.Should().OnlyContain(c => c.Weapon == weapon);
        }

        [Test]
        [Ignore("Target behaviour for the weapon-centric resolver refactor. Enable when the double-fire bug is fixed; then delete the characterization test above.")]
        public void AmplifierOnBothSides_ShouldProduceOneChainWithBothAmplifiers()
        {
            var ampA   = new FakeAmplifier("AmpA", Right);
            var weapon = new FakeWeapon("Weapon", Left, Right);
            var ampB   = new FakeAmplifier("AmpB", Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), ampA)
                .Place(new Vector2Int(1, 0), weapon)
                .Place(new Vector2Int(2, 0), ampB);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(1);
            chains[0].Modifiers.Should().Contain(new[] { ampA, ampB }.Cast<ITetrisItem>());
        }
    }
}

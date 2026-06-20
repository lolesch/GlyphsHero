using System.Linq;
using Code.Data.Enums;
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
            // Shifter<->Amplifier is illegal, so the amplifier is excluded. The weapon (no reactor)
            // is its own firing source; the shifter rides its timer as a modifier.
            var amp     = new FakeAmplifier("Amp", Right);
            var shifter = new FakeShifter("Shifter", Left, Right);
            var weapon  = new FakeWeapon("Weapon", Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), amp)
                .Place(new Vector2Int(1, 0), shifter)
                .Place(new Vector2Int(2, 0), weapon);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(1);
            chains[0].Root.Should().BeSameAs(weapon);
            chains[0].Modifiers.Should().Contain(shifter).And.NotContain(amp);
        }

        [Test]
        public void AmplifierOnBothSides_ProducesOneChainWithBothAmplifiers()
        {
            // [ampA]( -> )[weapon]( <- )[ampB]
            // One weapon source, one BFS in both directions → a single firing carrying both amps.
            // Replaces the old double-fire characterization (one chain per root connector).
            var ampA   = new FakeAmplifier("AmpA", Right);
            var weapon = new FakeWeapon("Weapon", Left, Right);
            var ampB   = new FakeAmplifier("AmpB", Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), ampA)
                .Place(new Vector2Int(1, 0), weapon)
                .Place(new Vector2Int(2, 0), ampB);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(1);
            chains[0].Weapon.Should().BeSameAs(weapon);
            chains[0].Modifiers.Should().Contain(ampA).And.Contain(ampB);
        }

        [Test]
        public void EquidistantReactors_OnDifferentEvents_BothFire()
        {
            // [reactorA]( -> )[weapon]( <- )[reactorB]
            // Two reactors equidistant from the weapon both drive it. The old furthest-trigger root
            // rule kept only one (nondeterministic tie) and silently dropped the other. Distinct
            // events so neither is deduplicated.
            var reactorA = new FakeReactor("ReactorA", ReactorType.OnSelfHit,    Right);
            var weapon   = new FakeWeapon("Weapon", Left, Right);
            var reactorB = new FakeReactor("ReactorB", ReactorType.OnEnemyDeath, Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), reactorA)
                .Place(new Vector2Int(1, 0), weapon)
                .Place(new Vector2Int(2, 0), reactorB);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(2);
            chains.Should().OnlyContain(c => c.Weapon == weapon);
            var roots = chains.Select(c => c.Root).ToList();
            roots.Should().Contain(reactorA);
            roots.Should().Contain(reactorB);
        }

        [Test]
        public void EquidistantReactors_OnSameEvent_FireOnce()
        {
            // Same event on both reactors → the weapon's trigger list holds it once → one firing.
            var reactorA = new FakeReactor("ReactorA", ReactorType.OnSelfHit, Right);
            var weapon   = new FakeWeapon("Weapon", Left, Right);
            var reactorB = new FakeReactor("ReactorB", ReactorType.OnSelfHit, Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), reactorA)
                .Place(new Vector2Int(1, 0), weapon)
                .Place(new Vector2Int(2, 0), reactorB);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(1);
            new ITetrisItem[] { reactorA, reactorB }.Should().Contain(chains[0].Root);
        }

        [Test]
        public void ParallelReactors_EachClaimsOnlyItsOwnSideShifter()
        {
            // [reactorA][shifterA][weapon][shifterB][reactorB]
            // Each reactor's firing walks toward the weapon and stops at the weapon's far-side
            // trigger wall — reactorA gets shifterA only, reactorB gets shifterB only.
            var reactorA = new FakeReactor("ReactorA", ReactorType.OnSelfHit,    Right);
            var shifterA = new FakeShifter("ShifterA", Left, Right);
            var weapon   = new FakeWeapon("Weapon", Left, Right);
            var shifterB = new FakeShifter("ShifterB", Left, Right);
            var reactorB = new FakeReactor("ReactorB", ReactorType.OnEnemyDeath, Left);
            var container = new FakeContainer(new Vector2Int(6, 1))
                .Place(new Vector2Int(0, 0), reactorA)
                .Place(new Vector2Int(1, 0), shifterA)
                .Place(new Vector2Int(2, 0), weapon)
                .Place(new Vector2Int(3, 0), shifterB)
                .Place(new Vector2Int(4, 0), reactorB);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(2);
            var fromA = chains.Single(c => c.Root == reactorA);
            var fromB = chains.Single(c => c.Root == reactorB);
            fromA.Modifiers.Should().Contain(shifterA).And.NotContain(shifterB);
            fromB.Modifiers.Should().Contain(shifterB).And.NotContain(shifterA);
        }

        [Test]
        public void ReactorPresent_SuppressesTimer_AndAmplifierRidesTheReactorFiring()
        {
            // [reactor][weapon][amp]: any reactor suppresses the weapon's timer, so there is no
            // weapon-rooted firing — the lone firing is reactor-rooted and still carries the amp.
            var reactor = new FakeReactor("Reactor", ReactorType.OnSelfHit, Right);
            var weapon  = new FakeWeapon("Weapon", Left, Right);
            var amp     = new FakeAmplifier("Amp", Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), reactor)
                .Place(new Vector2Int(1, 0), weapon)
                .Place(new Vector2Int(2, 0), amp);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(1);
            chains[0].Root.Should().BeSameAs(reactor);
            chains.Should().NotContain(c => c.Root is IWeaponItem);
            chains[0].Modifiers.Should().Contain(amp);
        }

        [Test]
        public void ReactorBetweenTwoWeapons_FiresEachWeaponSeparately()
        {
            // [weaponA][reactor][weaponB]: the reactor drives both weapons, but a different weapon
            // ends a branch — so each weapon is its own firing. Two firings, same root, distinct
            // weapons. (This is what ChainCollapser's keep-separate rule used to guarantee.)
            var weaponA = new FakeWeapon("WeaponA", Right);
            var reactor = new FakeReactor("Reactor", ReactorType.OnSelfHit, Left, Right);
            var weaponB = new FakeWeapon("WeaponB", Left);
            var container = new FakeContainer(new Vector2Int(4, 1))
                .Place(new Vector2Int(0, 0), weaponA)
                .Place(new Vector2Int(1, 0), reactor)
                .Place(new Vector2Int(2, 0), weaponB);

            var chains = ChainResolver.Resolve(container);

            chains.Should().HaveCount(2);
            chains.Should().OnlyContain(c => c.Root == reactor);
            var weapons = chains.Select(c => c.Weapon).ToList();
            weapons.Should().Contain(weaponA);
            weapons.Should().Contain(weaponB);
        }
    }
}

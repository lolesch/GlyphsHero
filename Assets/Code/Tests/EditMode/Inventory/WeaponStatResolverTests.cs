using System;
using System.Collections.Generic;
using Code.Data.Enums;
using Code.Runtime.Modules.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Inventory
{
    /// <summary>
    /// Behavioural lock on WeaponStatResolver — the single rule that turns a weapon plus its
    /// chained modifiers into a computed <see cref="WeaponStats"/> value, with no weapon mutation.
    /// This is the seam combat and the tooltip both read; these tests are the first time chain
    /// stats are exercised in isolation.
    /// </summary>
    [TestFixture]
    public sealed class WeaponStatResolverTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void LoneWeapon_ResolvesToWeaponBaseStats()
        {
            var weapon = new FakeWeapon("Weapon"); // base: dmg 1, spd 1, cost 0, gen 0

            var stats = WeaponStatResolver.Resolve(weapon, Array.Empty<ITetrisItem>());

            stats.Damage.Should().BeApproximately(1f, Tolerance);
            stats.AttackSpeed.Should().BeApproximately(1f, Tolerance);
            stats.ResourceCost.Should().BeApproximately(0f, Tolerance);
            stats.ResourceGenOnHit.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void AmplifierOnDamage_RaisesDamageOnly()
        {
            var weapon = new FakeWeapon("Weapon"); // dmg 1
            var amp    = new FakeAmplifier("Amp");  // outputMod: Damage +1 (FlatAdd)

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { amp });

            stats.Damage.Should().BeApproximately(2f, Tolerance);
            stats.AttackSpeed.Should().BeApproximately(1f, Tolerance);
            stats.ResourceCost.Should().BeApproximately(0f, Tolerance);
            stats.ResourceGenOnHit.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void AmplifierOnResourceGenOnHit_RaisesGenOnly()
        {
            var weapon = new StatWeapon(damage: 5f, resourceGenOnHit: 1f);
            var amp    = new StatAmplifier(Mods.Output(WeaponOutputStat.ResourceGenOnHit, Mods.Flat(2f)));

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { amp });

            stats.ResourceGenOnHit.Should().BeApproximately(3f, Tolerance);
            stats.Damage.Should().BeApproximately(5f, Tolerance);
            stats.AttackSpeed.Should().BeApproximately(0f, Tolerance);
            stats.ResourceCost.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void TwoAmplifiersOnDamage_Sum()
        {
            var weapon = new StatWeapon(damage: 1f);
            var a1     = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Flat(2f)));
            var a2     = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Flat(3f)));

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { a1, a2 });

            stats.Damage.Should().BeApproximately(6f, Tolerance);
        }

        [Test]
        public void Shifter_AppliesInputModToInputStat_AndOutputModToOutputStat()
        {
            // The drift-killer: combat historically pushed the shifter's INPUT value onto the
            // OUTPUT stat. The one rule is inputMod → input stat, outputMod → output stat. Distinct
            // values (+2 vs +5) so a test can only pass if each mod lands on its own stat.
            var weapon  = new StatWeapon(damage: 10f, attackSpeed: 1f);
            var shifter = new StatShifter(
                Mods.Input(WeaponInputStat.AttackSpeed, Mods.Flat(2f)),
                Mods.Output(WeaponOutputStat.Damage,    Mods.Flat(5f)));

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { shifter });

            stats.AttackSpeed.Should().BeApproximately(3f, Tolerance);  // 1 + 2 (inputMod)
            stats.Damage.Should().BeApproximately(15f, Tolerance);      // 10 + 5 (outputMod, NOT inputMod's 2)
        }

        [Test]
        public void Shifter_InputModOnManaCost_RaisesResourceCost()
        {
            var weapon  = new StatWeapon(resourceCost: 5f);
            var shifter = new StatShifter(
                Mods.Input(WeaponInputStat.ManaCost,   Mods.Flat(3f)),
                Mods.Output(WeaponOutputStat.Damage,   Mods.Flat(0f)));

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { shifter });

            stats.ResourceCost.Should().BeApproximately(8f, Tolerance);
        }

        [Test]
        public void Reactor_InputModOnAttackSpeed_RaisesAttackSpeed()
        {
            var weapon  = new StatWeapon(attackSpeed: 1f);
            var reactor = new StatReactor(Mods.Input(WeaponInputStat.AttackSpeed, Mods.Flat(4f)));

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { reactor });

            stats.AttackSpeed.Should().BeApproximately(5f, Tolerance);
        }

        [Test]
        public void PercentModifiers_AggregateAfterFlat_RegardlessOfOrder()
        {
            // Locks that resolution defers to MutableFloat's aggregation (flat first, then percent),
            // not hand-rolled arithmetic — even when the percent modifier is supplied first.
            var weapon  = new StatWeapon(damage: 10f);
            var percent = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Percent(50f)));
            var flat    = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Flat(10f)));

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { percent, flat });

            stats.Damage.Should().BeApproximately(30f, Tolerance);  // (10 + 10) * (1 + 0.5)
        }

        [Test]
        public void UnbackedInputStat_IsIgnored_NotThrown()
        {
            // LifeCost/ProcChance have no WeaponStats field. WeaponUtils throws for them; the resolver
            // must instead drop them silently so an exotic shifter never crashes combat.
            var weapon  = new StatWeapon(damage: 5f, attackSpeed: 1f);
            var shifter = new StatShifter(
                Mods.Input(WeaponInputStat.LifeCost, Mods.Flat(99f)),
                Mods.Output(WeaponOutputStat.Damage, Mods.Flat(0f)));

            Action act = () => WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { shifter });

            act.Should().NotThrow();
            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { shifter });
            stats.Damage.Should().BeApproximately(5f, Tolerance);
            stats.AttackSpeed.Should().BeApproximately(1f, Tolerance);
            stats.ResourceCost.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void ResolveChain_FoldsInBothRootAndModifiers()
        {
            // The chain overload is the seam combat reads. A trigger root's mods are NOT in
            // Modifiers (ChainResolver excludes the root), so the overload must apply the root too.
            var weapon  = new StatWeapon(damage: 10f, attackSpeed: 1f);
            var amp     = new StatAmplifier(Mods.Output(WeaponOutputStat.Damage, Mods.Flat(5f)));
            var reactor = new StatReactor(Mods.Input(WeaponInputStat.AttackSpeed, Mods.Flat(4f)));
            var chain   = new ItemChain(reactor, new List<ITetrisItem> { weapon, amp });

            var stats = WeaponStatResolver.Resolve(chain);

            stats.Damage.Should().BeApproximately(15f, Tolerance);     // 10 base + 5 amp (modifier)
            stats.AttackSpeed.Should().BeApproximately(5f, Tolerance); // 1 base + 4 reactor (root)
        }
    }
}

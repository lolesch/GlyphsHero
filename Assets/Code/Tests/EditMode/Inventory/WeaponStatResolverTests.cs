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
            var weapon = new FakeWeapon("Weapon"); // base: dmg 1, spd 1, cost 0

            var stats = WeaponStatResolver.Resolve(weapon, Array.Empty<ITetrisItem>());

            stats.Damage.Should().BeApproximately(1f, Tolerance);
            stats.AttackSpeed.Should().BeApproximately(1f, Tolerance);
            stats.ResourceCost.Should().BeApproximately(0f, Tolerance);
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
        public void UnbackedInputStat_ProcChance_IsIgnored_NotThrown()
        {
            // ProcChance has no WeaponStats field — drop it silently so an exotic shifter never crashes.
            var weapon  = new StatWeapon(damage: 5f, attackSpeed: 1f);
            var shifter = new StatShifter(
                Mods.Input(WeaponInputStat.ProcChance, Mods.Flat(99f)),
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

        // ── Converter: type-reclassification on one axis (ADR-0004 §1) ──────────────

        [Test]
        public void Converter_Delivery_ReclassifiesPattern_NotAmount()
        {
            // The Converter changes the *kind* (Single → Cleave), never the *amount* — Damage is
            // untouched, distinguishing it from the Amplifier.
            var weapon    = new StatWeapon(damage: 7f); // base delivery Single (StatWeapon default)
            var converter = new StatConverter(ConverterAxis.Delivery, toDelivery: DeliveryPattern.Cleave);

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { converter });

            stats.Delivery.Should().Be(DeliveryPattern.Cleave);
            stats.Damage.Should().BeApproximately(7f, Tolerance);
        }

        [Test]
        public void Converter_Affinity_ReclassifiesSide()
        {
            var weapon    = new StatWeapon(); // base affinity Hostile
            var converter = new StatConverter(ConverterAxis.Affinity, toAffinity: Affinity.Friendly);

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { converter });

            stats.Affinity.Should().Be(Affinity.Friendly);
        }

        [Test]
        public void Converter_Anchor_ReclassifiesCentre()
        {
            var weapon    = new StatWeapon(); // base anchor Target
            var converter = new StatConverter(ConverterAxis.Anchor, toAnchor: Anchor.Origin);

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { converter });

            stats.Anchor.Should().Be(Anchor.Origin);
        }

        [Test]
        public void Converter_OnlyTouchesItsOwnAxis()
        {
            // A Delivery converter carries To* values for every axis, but must apply only the one its
            // Axis selects — the other axes stay at the weapon's base (Hostile / Target).
            var weapon    = new StatWeapon();
            var converter = new StatConverter(ConverterAxis.Delivery,
                toDelivery: DeliveryPattern.Line, toAffinity: Affinity.Self, toAnchor: Anchor.Origin);

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { converter });

            stats.Delivery.Should().Be(DeliveryPattern.Line);
            stats.Affinity.Should().Be(Affinity.Hostile); // NOT the converter's Self
            stats.Anchor.Should().Be(Anchor.Target);      // NOT the converter's Origin
        }

        [Test]
        public void Converter_LastWins_OnSameAxis()
        {
            // Reclassification replaces (it is not magnitude that stacks). Two Delivery converters
            // leave the last one's value.
            var weapon = new StatWeapon();
            var first  = new StatConverter(ConverterAxis.Delivery, toDelivery: DeliveryPattern.Cleave);
            var second = new StatConverter(ConverterAxis.Delivery, toDelivery: DeliveryPattern.Line);

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { first, second });

            stats.Delivery.Should().Be(DeliveryPattern.Line);
        }

        // ── Converter: Resource axis — cost-pool reclassification (ADR-0005 §2) ──────────

        [Test]
        public void Converter_Resource_ReclassifiesCostPool()
        {
            // Blood-magic: a Mana-cost weapon + a Resource Converter → spends Health instead.
            // The Converter changes the *kind* (Mana → Health), never the *amount* — Damage untouched.
            var weapon    = new StatWeapon(damage: 5f, resourceCost: 3f); // CostResource = Mana (default)
            var converter = new StatConverter(ConverterAxis.Resource, toResource: ResourceType.Health);

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { converter });

            stats.CostResource.Should().Be(ResourceType.Health);
            stats.Damage.Should().BeApproximately(5f, Tolerance);        // amount unchanged
            stats.ResourceCost.Should().BeApproximately(3f, Tolerance);  // magnitude unchanged
        }

        [Test]
        public void Converter_Resource_OnlyTouchesItsOwnAxis()
        {
            // A Resource converter must not bleed into Delivery, Affinity, or Anchor.
            var weapon    = new StatWeapon();
            var converter = new StatConverter(ConverterAxis.Resource,
                toDelivery: DeliveryPattern.Cleave, toAffinity: Affinity.Self,
                toAnchor: Anchor.Origin, toResource: ResourceType.Health);

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { converter });

            stats.CostResource.Should().Be(ResourceType.Health);
            stats.Delivery.Should().Be(DeliveryPattern.Single);   // NOT the converter's Cleave
            stats.Affinity.Should().Be(Affinity.Hostile);         // NOT the converter's Self
            stats.Anchor.Should().Be(Anchor.Target);              // NOT the converter's Origin
        }

        [Test]
        public void Converter_Resource_LoneWeapon_DefaultsToMana()
        {
            // A weapon with no Converter keeps its authored CostResource (default Mana).
            var weapon = new StatWeapon();

            var stats = WeaponStatResolver.Resolve(weapon, Array.Empty<ITetrisItem>());

            stats.CostResource.Should().Be(ResourceType.Mana);
        }
    }
}

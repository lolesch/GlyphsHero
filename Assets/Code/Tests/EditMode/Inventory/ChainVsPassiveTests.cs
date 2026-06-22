using System;
using System.Linq;
using Code.Data.Enums;
using Code.Runtime.Modules.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Inventory
{
    /// <summary>
    /// Locks the chaining-vs-passive tradeoff that the amplifier configs must make worthwhile: a
    /// dual-purpose attachment (the real AmplifierItem is both an IAmplifierItem and an
    /// IAttachmentItem) pays out in WEAPON stats when chained and in PAWN stats when loose — and the
    /// two payoffs are mutually exclusive. A regression here is what made chaining worthless in play
    /// (the configs' chained Damage modifier had been reset to Overwrite 0, zeroing the weapon).
    /// </summary>
    [TestFixture]
    public sealed class ChainVsPassiveTests
    {
        // First item is the chain root, the rest are downstream modifiers — all count as "chained".
        private static IItemChain Chain(params ITetrisItem[] items)
            => new ItemChain(items[0], items.Skip(1).ToList());

        [Test]
        public void Chained_BoostsWeaponDamage()
        {
            var weapon = new FakeWeapon("W");                       // base damage 1
            var amp    = new FakeDualAmplifier("Amp", damageBonus: 2f);

            var stats = WeaponStatResolver.Resolve(weapon, new ITetrisItem[] { amp });

            stats.Damage.Should().Be((float)weapon.Damage + 2f);
        }

        [Test]
        public void Chained_GrantsNoPassive()
        {
            var weapon    = new FakeWeapon("W");
            var amp       = new FakeDualAmplifier("Amp");
            var container = new FakeStateContainer().Add(weapon).Add(amp);
            container.SetChains(Chain(weapon, amp));
            var stats = new RecordingStats();

            _ = new ChainStateController(container, stats);

            stats.Active.Should().BeEmpty("a chained amplifier pays out in weapon stats, not pawn stats");
        }

        [Test]
        public void Loose_GrantsPassive_AndLeavesWeaponUnchanged()
        {
            var weapon    = new FakeWeapon("W");
            var amp       = new FakeDualAmplifier("Amp", damageBonus: 2f, lifeBonus: 5f);
            var container = new FakeStateContainer().Add(weapon).Add(amp); // amp loose, not in a chain
            var stats     = new RecordingStats();

            _ = new ChainStateController(container, stats);

            stats.Active.Should().ContainSingle().Which.Should().Be(amp.Affix);
            // The loose amplifier is not a contributor, so the weapon keeps its base damage.
            WeaponStatResolver.Resolve(weapon, Array.Empty<ITetrisItem>())
                .Damage.Should().Be((float)weapon.Damage);
        }
    }
}

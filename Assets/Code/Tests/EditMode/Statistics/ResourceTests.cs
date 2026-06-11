using System;
using Code.Data.Enums;
using Code.Runtime.Modules.Statistics;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Statistics
{
    /// <summary>
    /// Resource clamping, spend rules, and the documented "max-mod changes current"
    /// behaviour (see KNOWN_ISSUES.md). These are characterization tests — they assert
    /// what the code does TODAY so a future fix has a clear baseline to change.
    /// </summary>
    [TestFixture]
    public sealed class ResourceTests
    {
        private static Modifier Mod(float value, ModifierType type) =>
            new(value, type, Guid.NewGuid());

        [Test]
        public void NewResource_StartsFull()
        {
            var mana = new Resource(PawnStat.ManaMax, 100f);

            mana.CurrentValue.Should().Be(100f);
            mana.IsFull.Should().BeTrue();
            mana.IsDepleted.Should().BeFalse();
        }

        [Test]
        public void ReduceCurrent_LowersCurrentAndReturnsUnspentRemainder()
        {
            var mana = new Resource(PawnStat.ManaMax, 100f);

            var remainder = mana.ReduceCurrent(30f);

            mana.CurrentValue.Should().Be(70f);
            remainder.Should().Be(0f);
        }

        [Test]
        public void ReduceCurrent_BelowZero_ClampsAndReportsRemainder()
        {
            var mana = new Resource(PawnStat.ManaMax, 100f);

            var remainder = mana.ReduceCurrent(130f);

            mana.CurrentValue.Should().Be(0f);
            mana.IsDepleted.Should().BeTrue();
            remainder.Should().Be(30f);
        }

        [Test]
        public void IncreaseCurrent_AboveMax_ClampsToMax()
        {
            var mana = new Resource(PawnStat.ManaMax, 100f);
            mana.ReduceCurrent(80f); // current = 20

            var remainder = mana.IncreaseCurrent(1000f);

            mana.CurrentValue.Should().Be(100f);
            remainder.Should().Be(920f);
        }

        [Test]
        public void CanSpend_ManaResource_AllowsSpendingExactlyCurrent()
        {
            var mana = new Resource(PawnStat.ManaMax, 100f);

            mana.CanSpend(100f).Should().BeTrue();
            mana.CanSpend(100.01f).Should().BeFalse();
        }

        [Test]
        public void CanSpend_LifeResource_ForbidsSpendingExactlyCurrent()
        {
            // Health-as-resource must never self-deplete to 0 via a cost.
            var health = new Resource(PawnStat.LifeMax, 100f);

            health.CanSpend(99.99f).Should().BeTrue();
            health.CanSpend(100f).Should().BeFalse();
        }

        [Test]
        public void RaisingMaxWhileFull_DoesNotRaiseCurrent()
        {
            // Documents current behaviour: growing max leaves current where it was,
            // so a previously-full resource becomes non-full rather than scaling up.
            var mana = new Resource(PawnStat.ManaMax, 100f);

            mana.AddModifier(Mod(50f, ModifierType.FlatAdd)); // max -> 150

            mana.CurrentValue.Should().Be(100f);
            mana.IsFull.Should().BeFalse();
        }

        [Test]
        public void LoweringMaxBelowCurrent_ClampsCurrentDown()
        {
            var mana = new Resource(PawnStat.ManaMax, 100f);
            var shrink = Mod(-60f, ModifierType.FlatAdd);

            mana.AddModifier(shrink); // max -> 40, current clamps to 40
            mana.CurrentValue.Should().Be(40f);

            mana.TryRemoveModifier(shrink); // max -> 100, current stays 40
            mana.CurrentValue.Should().Be(40f);
        }
    }
}

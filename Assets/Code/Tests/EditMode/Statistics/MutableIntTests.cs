using System;
using Code.Data.Enums;
using Code.Runtime.Modules.Statistics;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Statistics
{
    /// <summary>
    /// <see cref="MutableInt"/> wraps a <see cref="MutableFloat"/> so int-valued stats can still blend
    /// fractional modifiers (PercentAdd/PercentMult) in float space, rounding to int only at the read
    /// boundary. Its internal field used to be mistyped as <c>MutableInt</c> instead of
    /// <c>MutableFloat</c>, so the constructor called itself forever — an unrecoverable
    /// <see cref="StackOverflowException"/> on the very first construction, for every caller, always.
    /// That crash can't be reproduced here as a normal red-green "fails today" run (a real stack
    /// overflow in .NET kills the process, not just the test) — these tests only lock the fixed
    /// behavior going forward.
    /// </summary>
    [TestFixture]
    public sealed class MutableIntTests
    {
        private static Modifier Mod(float value, ModifierType type) =>
            new(value, type, Guid.NewGuid());

        [Test]
        public void Construction_DoesNotOverflowAndReturnsBase()
        {
            var stat = new MutableInt(10);

            ((int)stat).Should().Be(10);
        }

        [Test]
        public void FlatAdd_AddsToBase_RoundedToInt()
        {
            var stat = new MutableInt(10);

            stat.AddModifier(Mod(5f, ModifierType.FlatAdd));

            ((int)stat).Should().Be(15);
        }

        [Test]
        public void PercentAdd_BlendsInFloatSpace_ThenRoundsToInt()
        {
            var stat = new MutableInt(10);

            stat.AddModifier(Mod(55f, ModifierType.PercentAdd)); // 10 * 1.55 = 15.5 -> rounds to 16

            ((int)stat).Should().Be(16);
        }

        // ── RoundingMode: default is Nearest; Floor/Ceil are opt-in via the constructor ──

        [Test]
        public void DefaultConstructor_UsesNearestRounding()
        {
            var stat = new MutableInt(9);

            stat.AddModifier(Mod(20f, ModifierType.PercentAdd)); // 9 * 1.2 = 10.8 -> nearest = 11

            ((int)stat).Should().Be(11);
        }

        [Test]
        public void Floor_TruncatesDownEvenWhenNearestWouldRoundUp()
        {
            var stat = new MutableInt(9, RoundingMode.Floor);

            stat.AddModifier(Mod(20f, ModifierType.PercentAdd)); // 9 * 1.2 = 10.8 -> floor = 10

            ((int)stat).Should().Be(10);
        }

        [Test]
        public void Ceil_RoundsUpEvenWhenNearestWouldRoundDown()
        {
            var stat = new MutableInt(10, RoundingMode.Ceil);

            stat.AddModifier(Mod(2f, ModifierType.PercentAdd)); // 10 * 1.02 = 10.2 -> nearest = 10, ceil = 11

            ((int)stat).Should().Be(11);
        }

        [Test]
        public void TryRemoveModifier_RevertsItsContribution()
        {
            var stat = new MutableInt(10);
            var flat = Mod(5f, ModifierType.FlatAdd);
            stat.AddModifier(flat);

            stat.TryRemoveModifier(flat).Should().BeTrue();

            ((int)stat).Should().Be(10);
        }

        [Test]
        public void TryRemoveModifier_NotPresent_ReturnsFalse()
        {
            var stat = new MutableInt(10);

            stat.TryRemoveModifier(Mod(1f, ModifierType.FlatAdd)).Should().BeFalse();
        }

        [Test]
        public void OnTotalChanged_FiresWithRoundedTotal()
        {
            var stat = new MutableInt(10);
            int? observed = null;
            stat.OnTotalChanged += v => observed = v;

            stat.AddModifier(Mod(5f, ModifierType.FlatAdd));

            observed.Should().Be(15);
        }
    }
}

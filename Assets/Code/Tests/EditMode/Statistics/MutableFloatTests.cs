using System;
using Code.Data.Enums;
using Code.Runtime.Modules.Statistics;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Statistics
{
    /// <summary>
    /// Locks in the modifier math of the stat engine: application order
    /// (FlatAdd -> PercentAdd -> PercentMult), Overwrite precedence, and removal.
    /// </summary>
    [TestFixture]
    public sealed class MutableFloatTests
    {
        private static Modifier Mod(float value, ModifierType type) =>
            new(value, type, Guid.NewGuid());

        [Test]
        public void BaseValue_WithNoModifiers_ReturnsBase()
        {
            var stat = new MutableFloat(100f);

            ((float)stat).Should().Be(100f);
        }

        [Test]
        public void FlatAdd_AddsToBase()
        {
            var stat = new MutableFloat(100f);

            stat.AddModifier(Mod(10f, ModifierType.FlatAdd));

            ((float)stat).Should().BeApproximately(110f, 0.0001f);
        }

        [Test]
        public void Modifiers_ApplyInOrder_FlatThenPercentAddThenPercentMult()
        {
            var stat = new MutableFloat(100f);

            stat.AddModifier(Mod(10f, ModifierType.FlatAdd));    // 110
            stat.AddModifier(Mod(50f, ModifierType.PercentAdd)); // 110 * 1.5 = 165
            stat.AddModifier(Mod(100f, ModifierType.PercentMult)); // 165 * 2 = 330

            ((float)stat).Should().BeApproximately(330f, 0.0001f);
        }

        [Test]
        public void Overwrite_TakesPrecedenceOverEverythingElse()
        {
            var stat = new MutableFloat(100f);

            stat.AddModifier(Mod(10f, ModifierType.FlatAdd));
            stat.AddModifier(Mod(50f, ModifierType.PercentAdd));
            stat.AddModifier(Mod(42f, ModifierType.Overwrite));

            ((float)stat).Should().Be(42f);
        }

        [Test]
        public void TryRemoveModifier_RevertsItsContribution()
        {
            var stat = new MutableFloat(100f);
            var flat = Mod(25f, ModifierType.FlatAdd);

            stat.AddModifier(flat);
            ((float)stat).Should().BeApproximately(125f, 0.0001f);

            stat.TryRemoveModifier(flat).Should().BeTrue();
            ((float)stat).Should().BeApproximately(100f, 0.0001f);
        }

        [Test]
        public void TryRemoveModifier_NotPresent_ReturnsFalse()
        {
            var stat = new MutableFloat(100f);

            stat.TryRemoveModifier(Mod(5f, ModifierType.FlatAdd)).Should().BeFalse();
        }

        [Test]
        public void OnTotalChanged_FiresWhenTotalChanges()
        {
            var stat = new MutableFloat(100f);
            float? observed = null;
            stat.OnTotalChanged += v => observed = v;

            stat.AddModifier(Mod(10f, ModifierType.FlatAdd));

            observed.Should().BeApproximately(110f, 0.0001f);
        }

        // ── Clone: an independent "what if" probe (tooltip issue #22 groundwork) ──

        [Test]
        public void Clone_CarriesTheSameModifiersAndTotal()
        {
            var stat = new MutableFloat(100f);
            stat.AddModifier(Mod(10f, ModifierType.FlatAdd));

            var clone = stat.Clone();

            ((float)clone).Should().BeApproximately(110f, 0.0001f);
        }

        [Test]
        public void Clone_RemovingAModifierOnTheClone_LeavesTheOriginalUntouched()
        {
            var stat = new MutableFloat(100f);
            var flat = Mod(10f, ModifierType.FlatAdd);
            stat.AddModifier(flat);

            var clone = stat.Clone();
            clone.TryRemoveModifier(flat).Should().BeTrue();

            ((float)clone).Should().BeApproximately(100f, 0.0001f);   // clone: "before" this modifier
            ((float)stat).Should().BeApproximately(110f, 0.0001f);    // original: unaffected
        }

        [Test]
        public void Clone_DoesNotCarryOverSubscribers()
        {
            var stat = new MutableFloat(100f);
            var fired = false;
            stat.OnTotalChanged += _ => fired = true;

            var clone = stat.Clone();
            clone.AddModifier(Mod(10f, ModifierType.FlatAdd));

            fired.Should().BeFalse();
        }

        // ── TryRemoveModifier(modifier, warnIfMissing): quiet probe variant ──

        [Test]
        public void TryRemoveModifier_QuietOverload_StillRemovesAPresentModifier()
        {
            var stat = new MutableFloat(100f);
            var flat = Mod(10f, ModifierType.FlatAdd);
            stat.AddModifier(flat);

            stat.TryRemoveModifier(flat, warnIfMissing: false).Should().BeTrue();
            ((float)stat).Should().BeApproximately(100f, 0.0001f);
        }

        [Test]
        public void TryRemoveModifier_QuietOverload_NotPresent_ReturnsFalseWithoutLogging()
        {
            var stat = new MutableFloat(100f);

            stat.TryRemoveModifier(Mod(5f, ModifierType.FlatAdd), warnIfMissing: false).Should().BeFalse();
        }
    }
}

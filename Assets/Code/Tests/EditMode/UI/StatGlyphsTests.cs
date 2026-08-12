using Code.Runtime.UI.Inventory;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the universal stat channel (tooltip-redesign v2 slice 8, issue #24): <see cref="StatGlyphs.For"/>
    /// maps every <see cref="StatKind"/> to a non-empty glyph, mirroring <see cref="TypeGlyphs"/>/
    /// <see cref="StateGlyphs"/>'s pattern; <see cref="StatGlyphs.Format"/> composes glyph + value in
    /// default mode and glyph + value + the stat's own name in Details mode. Expectations track whichever
    /// set shipped via <see cref="StatGlyphs.UseAsciiFallback"/>, so a human flipping that constant in Unity
    /// does not turn these tests red.
    ///
    /// Red-green: each glyph case asserts a distinct, non-empty return value, so a wrong/missing arm (e.g.
    /// a stat kind falling through to the default empty string) fails its own test; the Format cases pin
    /// the exact default vs Details shape, so dropping the Details-mode label (or adding it by mistake in
    /// default mode) fails immediately.
    /// </summary>
    [TestFixture]
    public sealed class StatGlyphsTests
    {
        private static bool Ascii => StatGlyphs.UseAsciiFallback;

        [Test]
        public void Damage_GlyphIsDamage() =>
            StatGlyphs.For(StatKind.Damage).Should().Be(Ascii ? "DMG" : "✺");

        [Test]
        public void Cost_GlyphIsCost() =>
            StatGlyphs.For(StatKind.Cost).Should().Be(Ascii ? "CST" : "$");

        [Test]
        public void AttackSpeed_GlyphIsAttackSpeed() =>
            StatGlyphs.For(StatKind.AttackSpeed).Should().Be(Ascii ? "SPD" : "⏱");

        [Test]
        public void ProcChance_GlyphIsProcChance() =>
            StatGlyphs.For(StatKind.ProcChance).Should().Be(Ascii ? "PROC" : "%");

        [Test]
        public void LifeMax_GlyphIsLifeMax() =>
            StatGlyphs.For(StatKind.LifeMax).Should().Be(Ascii ? "HP" : "♥");

        [Test]
        public void LifeRegen_GlyphIsLifeRegen() =>
            StatGlyphs.For(StatKind.LifeRegen).Should().Be(Ascii ? "HP+" : "♡");

        [Test]
        public void ManaMax_GlyphIsManaMax() =>
            StatGlyphs.For(StatKind.ManaMax).Should().Be(Ascii ? "MP" : "✦");

        [Test]
        public void ManaRegen_GlyphIsManaRegen() =>
            StatGlyphs.For(StatKind.ManaRegen).Should().Be(Ascii ? "MP+" : "✧");

        [Test]
        public void MovementSpeed_GlyphIsMovementSpeed() =>
            StatGlyphs.For(StatKind.MovementSpeed).Should().Be(Ascii ? "MOV" : "➤");

        [Test]
        public void Range_GlyphIsRange() =>
            StatGlyphs.For(StatKind.Range).Should().Be(Ascii ? "RNG" : "◎");

        [Test]
        public void EveryStatKind_MapsToNonEmptyGlyph(
            [Values(StatKind.Damage, StatKind.Cost, StatKind.AttackSpeed, StatKind.ProcChance,
                StatKind.LifeMax, StatKind.LifeRegen, StatKind.ManaMax, StatKind.ManaRegen,
                StatKind.MovementSpeed, StatKind.Range)]
            StatKind kind) =>
            StatGlyphs.For(kind).Should().NotBeNullOrEmpty();

        // ── Format: default is glyph+value, Details adds the stat's own name ──

        [Test]
        public void Format_Default_IsGlyphThenValue_NoLabel() =>
            StatGlyphs.Format(StatKind.LifeMax, "+10", detailed: false)
                .Should().Be($"{StatGlyphs.For(StatKind.LifeMax)} +10");

        [Test]
        public void Format_Details_AppendsStatNameAfterValue() =>
            StatGlyphs.Format(StatKind.LifeMax, "+10", detailed: true)
                .Should().Be($"{StatGlyphs.For(StatKind.LifeMax)} +10 Life Max");

        [Test]
        public void Format_Details_AddsLabelEvenWhenValueIsAlreadyAnEquation() =>
            // The label follows the detailed flag alone — it doesn't matter whether valueText is a plain
            // modifier or an already-expanded "base → result" equation (that expansion is the caller's job).
            StatGlyphs.Format(StatKind.Damage, "5.0 → 7.0", detailed: true)
                .Should().Be($"{StatGlyphs.For(StatKind.Damage)} 5.0 → 7.0 Damage");
    }
}

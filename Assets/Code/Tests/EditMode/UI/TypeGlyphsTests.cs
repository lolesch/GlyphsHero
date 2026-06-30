using Code.Data.Enums;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the type channel (tooltip-redesign slice 1): <see cref="TypeGlyphs.For"/> maps each item
    /// role to its own glyph, with the weapon split by the payload flag. Expectations track whichever
    /// set shipped via <see cref="TypeGlyphs.UseAsciiFallback"/>, so a human flipping that constant in
    /// Unity (because the Unicode glyphs aren't in the TMP atlas) does not turn these tests red.
    ///
    /// Red-green: each case asserts a distinct return value, so a wrong/missing arm (e.g. amplifier
    /// falling through to the default empty string, or weapon ignoring isPayload) fails its own test.
    /// </summary>
    [TestFixture]
    public sealed class TypeGlyphsTests
    {
        private static bool Ascii => TypeGlyphs.UseAsciiFallback;

        [Test]
        public void Weapon_Driving_GlyphIsWeapon() =>
            TypeGlyphs.For(new FakeWeapon("w"), isPayload: false)
                .Should().Be(Ascii ? "W" : "⚔");

        [Test]
        public void Weapon_Payload_GlyphIsPayload() =>
            TypeGlyphs.For(new FakeWeapon("w"), isPayload: true)
                .Should().Be(Ascii ? "P" : "◈");

        [Test]
        public void Amplifier_GlyphIsAmplifier() =>
            TypeGlyphs.For(new FakeAmplifier("a"), isPayload: false)
                .Should().Be(Ascii ? "A" : "◆");

        [Test]
        public void Reactor_GlyphIsReactor() =>
            TypeGlyphs.For(new FakeReactor("r"), isPayload: false)
                .Should().Be(Ascii ? "R" : "▸");

        [Test]
        public void Shifter_GlyphIsShifter() =>
            TypeGlyphs.For(new FakeShifter("s"), isPayload: false)
                .Should().Be(Ascii ? "S" : "⇄");

        [Test]
        public void Converter_GlyphIsConverter() =>
            TypeGlyphs.For(new FakeConverter("c", ConverterAxis.Delivery), isPayload: false)
                .Should().Be(Ascii ? "C" : "↻");

        // The payload flag only reclassifies a weapon; an attachment's glyph is role-fixed regardless.
        [Test]
        public void Attachment_IgnoresPayloadFlag() =>
            TypeGlyphs.For(new FakeAmplifier("a"), isPayload: true)
                .Should().Be(Ascii ? "A" : "◆");
    }
}

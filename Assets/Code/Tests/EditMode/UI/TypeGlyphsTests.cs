using Code.Data.Enums;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the type channel (tooltip-redesign slice 1, migrated to TMP sprite tags by issue #27):
    /// <see cref="TypeGlyphs.For"/> maps each item role x chain-state to its own <c>&lt;sprite
    /// name="..."&gt;</c> tag against the <c>ItemTypeIcons</c> TMP Sprite Asset, with the weapon role
    /// split by the payload flag. Expectations track whichever set shipped via
    /// <see cref="TypeGlyphs.UseAsciiFallback"/>, so a human flipping that constant (e.g. some other
    /// Text component isn't wired with the sprite asset) does not turn these tests red.
    ///
    /// Red-green: each case asserts a distinct return value, so a wrong/missing arm (e.g. amplifier
    /// falling through to the default empty string, weapon ignoring isPayload, or any role ignoring
    /// isChained) fails its own test.
    /// </summary>
    [TestFixture]
    public sealed class TypeGlyphsTests
    {
        private static bool Ascii => TypeGlyphs.UseAsciiFallback;

        [Test]
        public void Weapon_Driving_Chained_GlyphIsWeaponChained() =>
            TypeGlyphs.For(new FakeWeapon("w"), isPayload: false, isChained: true)
                .Should().Be(Ascii ? "W" : "<sprite name=\"Weapon_Chained\">");

        [Test]
        public void Weapon_Driving_Unchained_GlyphIsWeaponUnchained() =>
            TypeGlyphs.For(new FakeWeapon("w"), isPayload: false, isChained: false)
                .Should().Be(Ascii ? "W" : "<sprite name=\"Weapon_Unchained\">");

        [Test]
        public void Weapon_Payload_Chained_GlyphIsPayloadChained() =>
            TypeGlyphs.For(new FakeWeapon("w"), isPayload: true, isChained: true)
                .Should().Be(Ascii ? "P" : "<sprite name=\"Payload_Chained\">");

        [Test]
        public void Weapon_Payload_Unchained_GlyphIsPayloadUnchained() =>
            TypeGlyphs.For(new FakeWeapon("w"), isPayload: true, isChained: false)
                .Should().Be(Ascii ? "P" : "<sprite name=\"Payload_Unchained\">");

        [Test]
        public void Amplifier_Chained_GlyphIsAmplifierChained() =>
            TypeGlyphs.For(new FakeAmplifier("a"), isPayload: false, isChained: true)
                .Should().Be(Ascii ? "A" : "<sprite name=\"Amplifier_Chained\">");

        [Test]
        public void Amplifier_Unchained_GlyphIsAmplifierUnchained() =>
            TypeGlyphs.For(new FakeAmplifier("a"), isPayload: false, isChained: false)
                .Should().Be(Ascii ? "A" : "<sprite name=\"Amplifier_Unchained\">");

        [Test]
        public void Reactor_Chained_GlyphIsReactorChained() =>
            TypeGlyphs.For(new FakeReactor("r"), isPayload: false, isChained: true)
                .Should().Be(Ascii ? "R" : "<sprite name=\"Reactor_Chained\">");

        [Test]
        public void Shifter_Chained_GlyphIsShifterChained() =>
            TypeGlyphs.For(new FakeShifter("s"), isPayload: false, isChained: true)
                .Should().Be(Ascii ? "S" : "<sprite name=\"Shifter_Chained\">");

        [Test]
        public void Converter_Chained_GlyphIsConverterChained() =>
            TypeGlyphs.For(new FakeConverter("c", ConverterAxis.Delivery), isPayload: false, isChained: true)
                .Should().Be(Ascii ? "C" : "<sprite name=\"Converter_Chained\">");

        // The payload flag only reclassifies a weapon; an attachment's glyph is role-fixed regardless.
        [Test]
        public void Attachment_IgnoresPayloadFlag() =>
            TypeGlyphs.For(new FakeAmplifier("a"), isPayload: true, isChained: true)
                .Should().Be(Ascii ? "A" : "<sprite name=\"Amplifier_Chained\">");
    }
}

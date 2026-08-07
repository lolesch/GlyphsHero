using Code.Runtime.UI.Inventory;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the state channel (issue #20, part 2): <see cref="StateGlyphs.For"/> maps every
    /// <see cref="ItemStateKind"/> to a non-empty glyph, mirroring <see cref="TypeGlyphs"/>' pattern.
    /// Expectations track whichever set shipped via <see cref="StateGlyphs.UseAsciiFallback"/>, so a human
    /// flipping that constant in Unity (because the Unicode glyphs aren't in the TMP atlas) does not turn
    /// these tests red.
    ///
    /// Red-green: each case asserts a distinct, non-empty return value, so a wrong/missing arm (e.g. a
    /// state kind falling through to the default empty string) fails its own test.
    /// </summary>
    [TestFixture]
    public sealed class StateGlyphsTests
    {
        private static bool Ascii => StateGlyphs.UseAsciiFallback;

        [Test]
        public void Chained_GlyphIsChained() =>
            StateGlyphs.For(ItemStateKind.Chained).Should().Be(Ascii ? "CH" : "⛓");

        [Test]
        public void Unchained_GlyphIsUnchained() =>
            StateGlyphs.For(ItemStateKind.Unchained).Should().Be(Ascii ? "UN" : "○");

        [Test]
        public void Driving_GlyphIsDriving() =>
            StateGlyphs.For(ItemStateKind.Driving).Should().Be(Ascii ? "W" : "⚔");

        [Test]
        public void Payload_GlyphIsPayload() =>
            StateGlyphs.For(ItemStateKind.Payload).Should().Be(Ascii ? "P" : "◈");

        [Test]
        public void EveryStateKind_MapsToNonEmptyGlyph(
            [Values(ItemStateKind.Chained, ItemStateKind.Unchained, ItemStateKind.Driving, ItemStateKind.Payload)]
            ItemStateKind kind) =>
            StateGlyphs.For(kind).Should().NotBeNullOrEmpty();
    }
}

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip's <b>state channel</b> (issue #20, part 2): a leading glyph naming which of an item's
    /// two states (<see cref="ItemStateKind"/>) a row shows, replacing the old plain-text labels
    /// ("chained" / "unchained" / "as driving weapon" / "as payload"). Mirrors <see cref="TypeGlyphs"/>'
    /// pattern exactly (pure static map, <see cref="UseAsciiFallback"/> single-flip escape hatch) — the
    /// map shape is the deliverable, not the specific characters. Driving/Payload intentionally reuse
    /// <see cref="TypeGlyphs"/>' own weapon-role glyphs (⚔/◈): they're the same distinction, just named
    /// from the state side instead of the type side.
    /// </summary>
    public static class StateGlyphs
    {
        // VERIFY in Unity: the Unicode glyphs below must exist in the tooltip TMP font atlas. If any
        // render as a missing-glyph box, flip this to true (no other change) to ship the ASCII set.
        public const bool UseAsciiFallback = false;

        public static string For(ItemStateKind kind) => UseAsciiFallback ? Ascii(kind) : Glyph(kind);

        private static string Glyph(ItemStateKind kind) => kind switch
        {
            ItemStateKind.Chained   => "⛓", // linked into a chain
            ItemStateKind.Unchained => "○", // loose, no chain
            ItemStateKind.Driving   => "⚔", // fires the chain (= TypeGlyphs' weapon-driving glyph)
            ItemStateKind.Payload   => "◈", // carried downstream (= TypeGlyphs' weapon-payload glyph)
            _                       => string.Empty,
        };

        private static string Ascii(ItemStateKind kind) => kind switch
        {
            ItemStateKind.Chained   => "CH",
            ItemStateKind.Unchained => "UN",
            ItemStateKind.Driving   => "W",
            ItemStateKind.Payload   => "P",
            _                       => string.Empty,
        };
    }
}

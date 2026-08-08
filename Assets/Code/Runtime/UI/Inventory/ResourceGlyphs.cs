using Code.Data.Enums;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The cost-line's resource-icon channel (tooltip-redesign v2 slice 7, issue #23): the glyph shown
    /// ahead of a resource/cost multiplier line (e.g. a Reactor's <c>×120%</c> mana trigger cost). Mirrors
    /// <see cref="TypeGlyphs"/>/<see cref="StateGlyphs"/>'s pattern (pure static map + TMP-font
    /// verification + ASCII fallback) rather than inventing a third icon system. <c>PawnResourceView</c>
    /// (the pawn resource bar) carries no text glyph today — it's a plain fill <c>Image</c> with no
    /// per-resource iconography to literally reuse — so this is the first text-glyph map for
    /// <see cref="ResourceType"/>.
    /// </summary>
    public static class ResourceGlyphs
    {
        // VERIFY in Unity: the Unicode glyphs below must exist in the tooltip TMP font atlas. If any
        // render as a missing-glyph box, flip this to true (no other change) to ship the ASCII set.
        public const bool UseAsciiFallback = false;

        public static string For(ResourceType resource) =>
            UseAsciiFallback ? Ascii(resource) : Glyph(resource);

        private static string Glyph(ResourceType resource) => resource switch
        {
            ResourceType.Mana   => "✦", // mana
            ResourceType.Health => "♥", // health
            _                   => string.Empty,
        };

        private static string Ascii(ResourceType resource) => resource switch
        {
            ResourceType.Mana   => "MP",
            ResourceType.Health => "HP",
            _                   => string.Empty,
        };
    }
}

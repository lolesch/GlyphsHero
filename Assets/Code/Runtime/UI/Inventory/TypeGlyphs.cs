using Code.Runtime.Modules.Inventory;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip's <b>type channel</b> (tooltip-redesign spec 2026-06-30, slice 1): a leading glyph
    /// that names an item's role, used both in the item's own header and in the weapon's piece list.
    /// Color is reserved for <em>direction</em> (green up / red down); <em>type</em> is the glyph's job.
    ///
    /// The map shape is the deliverable, not the specific characters. The primary set is the spec's
    /// Unicode glyphs; <see cref="UseAsciiFallback"/> flips the whole map to a safe ASCII set in one edit
    /// if any glyph is missing from the tooltip's TMP font atlas (must be verified in the Unity editor —
    /// see the slice's VERIFY note). The role distinction mirrors <see cref="ChainComponentColors"/> and
    /// the tooltip's own ComponentLabel ordering.
    /// </summary>
    public static class TypeGlyphs
    {
        // VERIFY in Unity: the Unicode glyphs below must exist in the tooltip TMP font atlas. If any
        // render as a missing-glyph box, flip this to true (no other change) to ship the ASCII set.
        public const bool UseAsciiFallback = false;

        /// <summary>
        /// The role glyph for <paramref name="item"/>. A weapon reads as a <em>payload</em> when
        /// <paramref name="isPayload"/> is true (a weapon downstream of another weapon in its chain);
        /// the caller owns that classification (the tooltip's IsPayload).
        /// </summary>
        public static string For(ITetrisItem item, bool isPayload) =>
            UseAsciiFallback ? Ascii(item, isPayload) : Glyph(item, isPayload);

        private static string Glyph(ITetrisItem item, bool isPayload) => item switch
        {
            IWeaponItem when isPayload => "◈", // ◈ payload
            IWeaponItem                => "⚔", // ⚔ weapon (driving)
            IAmplifierItem             => "◆", // ◆ amplifier
            IConverterItem             => "↻", // ↻ converter
            IShifterItem               => "⇄", // ⇄ shifter
            IReactorItem               => "▸", // ▸ reactor
            _                          => string.Empty,
        };

        private static string Ascii(ITetrisItem item, bool isPayload) => item switch
        {
            IWeaponItem when isPayload => "P",
            IWeaponItem                => "W",
            IAmplifierItem             => "A",
            IConverterItem             => "C",
            IShifterItem               => "S",
            IReactorItem               => "R",
            _                          => string.Empty,
        };
    }
}

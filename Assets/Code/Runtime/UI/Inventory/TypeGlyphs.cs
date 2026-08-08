using Code.Runtime.Modules.Inventory;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip's <b>type channel</b> (tooltip-redesign spec 2026-06-30, slice 1): a leading glyph
    /// that names an item's role, used both in the item's own header and in the weapon's piece list.
    /// Color is reserved for <em>direction</em> (green up / red down); <em>type</em> is the glyph's job.
    ///
    /// Migrated to TMP sprite tags (issue #27): the primary set renders <c>&lt;sprite
    /// name="&lt;Type&gt;_&lt;State&gt;"&gt;</c> against the <c>ItemTypeIcons</c> TMP Sprite Asset (6
    /// item-type roles x Chained/Unchained, extracted from Figma), which must be assigned as the
    /// tooltip's <c>TMP_Text.spriteAsset</c> for the tag to resolve to an image rather than literal
    /// text. <see cref="UseAsciiFallback"/> flips the whole map to a safe ASCII set in one edit if that
    /// wiring is ever missing on some other Text component. The role distinction mirrors
    /// <see cref="ChainComponentColors"/> and the tooltip's own ComponentLabel ordering.
    /// </summary>
    public static class TypeGlyphs
    {
        // Escape hatch: flip to true (no other change) to fall back to plain ASCII letters if the
        // ItemTypeIcons sprite asset isn't wired on some Text component that renders this map's output.
        public const bool UseAsciiFallback = false;

        /// <summary>
        /// The role glyph for <paramref name="item"/>. A weapon reads as a <em>payload</em> when
        /// <paramref name="isPayload"/> is true (a weapon downstream of another weapon in its chain);
        /// the caller owns that classification (the tooltip's IsPayload). <paramref name="isChained"/>
        /// selects the icon's Chained/Unchained variant — whether this specific item instance currently
        /// sits in a resolved chain.
        /// </summary>
        public static string For(ITetrisItem item, bool isPayload, bool isChained) =>
            UseAsciiFallback ? Ascii(item, isPayload) : Sprite(item, isPayload, isChained);

        private static string Sprite(ITetrisItem item, bool isPayload, bool isChained)
        {
            var type = TypeName(item, isPayload);
            if (type.Length == 0) return string.Empty;

            var state = isChained ? "Chained" : "Unchained";
            return $"<sprite name=\"{type}_{state}\">";
        }

        private static string TypeName(ITetrisItem item, bool isPayload) => item switch
        {
            IWeaponItem when isPayload => "Payload",
            IWeaponItem                => "Weapon",
            IAmplifierItem             => "Amplifier",
            IConverterItem             => "Converter",
            IShifterItem               => "Shifter",
            IReactorItem               => "Reactor",
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

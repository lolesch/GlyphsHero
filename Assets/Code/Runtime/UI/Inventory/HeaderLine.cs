using Code.Runtime.Modules.Inventory;
using Submodules.Utility.Extensions;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip header row (tooltip-redesign v2 slice 5, issue #21): name + type glyph always
    /// render; the type <em>text</em> label (e.g. "[Amplifier]") is hidden by default and only appears
    /// in Details mode (Leonid: "it belongs to the icon, not the name" — issue #28 later dropped the
    /// standalone header icon itself as low-value, but the type-label toggle this refers to stands).
    /// The type glyph (<see cref="TypeGlyphs"/>) is a separate, always-present channel, rendered via a
    /// TMP sprite tag (issue #27) that reflects the item's current chain membership.
    /// </summary>
    public static class HeaderLine
    {
        public static string Build(ITetrisItem item, bool isPayload, bool isWeaponRoot, bool detailed,
            bool isChained = false)
        {
            var typeGlyph = TypeGlyphs.For(item, isPayload, isChained);
            var name      = $"<align=left>{typeGlyph} <b>{item.Name}</b>";

            if (!detailed)
                return name + "</align>";

            var labelColor   = ChainComponentColors.GetColor(item, isWeaponRoot);
            var componentStr = $"[{ComponentLabel(item, isPayload)}]".Colored(labelColor);
            return $"{name}<align=right> {componentStr}</align>";
        }

        private static string ComponentLabel(ITetrisItem item, bool isPayload) => item switch
        {
            IWeaponItem when isPayload => "Payload",
            IWeaponItem                => "Weapon",
            IAmplifierItem             => "Amplifier",
            IConverterItem             => "Converter",
            IShifterItem               => "Shifter",
            IReactorItem               => "Reactor",
            _                          => item.GetType().Name,
        };
    }
}

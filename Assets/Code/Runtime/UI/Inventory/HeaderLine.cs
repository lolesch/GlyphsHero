using Code.Runtime.Modules.Inventory;
using Submodules.Utility.Extensions;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip header row (tooltip-redesign v2 slice 5, issue #21): name + type glyph always
    /// render; the type <em>text</em> label (e.g. "[Amplifier]") is hidden by default and only appears
    /// in Details mode, left of where the header icon sits (Leonid: "it belongs to the icon, not the
    /// name"). The type glyph (<see cref="TypeGlyphs"/>) is a separate, always-present channel and is
    /// unaffected by this toggle. The real header icon `Image` element itself is Unity-side wiring
    /// (<see cref="ItemTooltipController"/>) — this builder only produces the text row.
    /// </summary>
    public static class HeaderLine
    {
        public static string Build(ITetrisItem item, bool isPayload, bool isWeaponRoot, bool detailed)
        {
            var typeGlyph = TypeGlyphs.For(item, isPayload);
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

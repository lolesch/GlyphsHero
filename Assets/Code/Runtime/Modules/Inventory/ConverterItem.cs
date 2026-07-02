using Code.Data.Enums;
using Code.Data.Items.Converter;
using Code.Runtime.Modules.Statistics;

namespace Code.Runtime.Modules.Inventory
{
    public sealed class ConverterItem : AttachmentItem, IConverterItem
    {
        public ConverterAxis   Axis       { get; }
        public DeliveryPattern ToDelivery { get; }
        public Affinity        ToAffinity { get; }
        public Anchor          ToAnchor   { get; }
        public ResourceType    ToResource { get; }
        public WeaponInputModifier inputMod { get; }

        public ConverterItem(ConverterConfig config, RotationType rotation = RotationType.None) : base(config, rotation)
        {
            Axis       = config.Axis;
            ToDelivery = config.ToDelivery;
            ToAffinity = config.ToAffinity;
            ToAnchor   = config.ToAnchor;
            ToResource = config.ToResource;

            inputMod = new WeaponInputModifier(
                config.inputStatMod.stat,
                new Modifier(config.inputStatMod.value, config.inputStatMod.type, Guid));
        }
    }

    /// <summary>
    /// The type-reclassifier (ADR-0004 §1, ADR-0005 §2): changes the <em>kind</em> of the nearest
    /// upstream weapon's attack on one axis (<see cref="Axis"/>), never the amount.
    /// <see cref="WeaponStatResolver"/> reads <see cref="Axis"/> and applies the matching <c>To*</c>
    /// value (replace, last-wins). <see cref="inputMod"/> is the independent, optional Cost axis
    /// (ADR-0009) — unrelated to the reclassification above.
    /// </summary>
    public interface IConverterItem : ITetrisItem
    {
        ConverterAxis   Axis       { get; }
        DeliveryPattern ToDelivery { get; }
        Affinity        ToAffinity { get; }
        Anchor          ToAnchor   { get; }
        ResourceType    ToResource { get; }
        WeaponInputModifier inputMod { get; }
    }
}

using Code.Data.Enums;
using Code.Data.Items.Converter;

namespace Code.Runtime.Modules.Inventory
{
    public sealed class ConverterItem : AttachmentItem, IConverterItem
    {
        public ConverterAxis   Axis       { get; }
        public DeliveryPattern ToDelivery { get; }
        public Affinity        ToAffinity { get; }
        public Anchor          ToAnchor   { get; }

        public ConverterItem(ConverterConfig config, RotationType rotation = RotationType.None) : base(config, rotation)
        {
            Axis       = config.Axis;
            ToDelivery = config.ToDelivery;
            ToAffinity = config.ToAffinity;
            ToAnchor   = config.ToAnchor;
        }
    }

    /// <summary>
    /// The type-reclassifier (ADR-0004 §1): changes the <em>kind</em> of the nearest upstream weapon's
    /// attack on one axis (<see cref="Axis"/>), never the amount. <see cref="WeaponStatResolver"/> reads
    /// <see cref="Axis"/> and applies the matching <c>To*</c> value (replace, last-wins).
    /// </summary>
    public interface IConverterItem : ITetrisItem
    {
        ConverterAxis   Axis       { get; }
        DeliveryPattern ToDelivery { get; }
        Affinity        ToAffinity { get; }
        Anchor          ToAnchor   { get; }
    }
}

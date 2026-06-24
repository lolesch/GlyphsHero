using Code.Data.Enums;
using UnityEngine;

namespace Code.Data.Items.Converter
{
    [CreateAssetMenu(fileName = "ConverterConfig", menuName = Const.ItemConfig + "Converter")]
    public sealed class ConverterConfig : AttachmentItemConfig
    {
        // The Converter reclassifies the *kind* on one attack axis of the nearest upstream weapon
        // (ADR-0004 §1, Converter.md) — never the amount. v1 covers the three axes that exist as data
        // on WeaponStats: Delivery / Affinity / Anchor. Only the target value matching Axis is used;
        // WeaponStatResolver reads Axis and applies the corresponding To* value (replace, last-wins).
        // Resource-type / damage-type / target-strategy reclassification is deferred (no data system).
        [field: Header("Chained — reclassifies the upstream weapon")]
        [field: SerializeField] public ConverterAxis   Axis        { get; private set; } = ConverterAxis.Delivery;
        [field: SerializeField] public DeliveryPattern ToDelivery  { get; private set; } = DeliveryPattern.Single;
        [field: SerializeField] public Affinity        ToAffinity  { get; private set; } = Affinity.Hostile;
        [field: SerializeField] public Anchor          ToAnchor    { get; private set; } = Anchor.Target;
        [field: SerializeField] public ResourceType    ToResource  { get; private set; } = ResourceType.Mana;

        public override int MaxConnectors => 2;
    }
}
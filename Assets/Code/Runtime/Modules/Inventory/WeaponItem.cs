using Code.Data.Enums;
using Code.Data.Items.Weapon;
using Code.Runtime.Modules.Statistics;

namespace Code.Runtime.Modules.Inventory
{
    public sealed class WeaponItem : TetrisItem, IWeaponItem
    {
        public MutableFloat    Damage       { get; }
        public MutableFloat    AttackSpeed  { get; }
        public MutableFloat    ResourceCost { get; }
        public ResourceType    CostResource { get; }
        public DeliveryPattern Delivery     { get; }
        public Affinity        Affinity     { get; }
        public Anchor          Anchor       { get; }
        public PayloadBehavior Payload      { get; }

        public WeaponItem(WeaponConfig config, RotationType rotation = RotationType.None) : base(config, rotation)
        {
            Damage       = new MutableFloat(config.BaseDamage);
            AttackSpeed  = new MutableFloat(config.AttackSpeed);
            ResourceCost = new MutableFloat(config.ResourceCost);
            CostResource = config.CostResource;
            Delivery     = config.Delivery;
            Affinity     = config.Affinity;
            Anchor       = config.Anchor;
            Payload      = config.Payload;
        }
    }

    public interface IWeaponItem : ITetrisItem
    {
        MutableFloat         Damage       { get; }
        MutableFloat         AttackSpeed  { get; }
        MutableFloat         ResourceCost { get; }
        /// <summary>Which pool the weapon's Cost draws from (ADR-0005 §2). Reclassified by ConverterAxis.Resource.</summary>
        ResourceType         CostResource { get; }
        DeliveryPattern      Delivery     { get; }
        Affinity             Affinity     { get; }
        Anchor               Anchor       { get; }
        PayloadBehavior      Payload      { get; }
    }
}
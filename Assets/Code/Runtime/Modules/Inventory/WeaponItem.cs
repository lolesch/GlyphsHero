using Code.Data.Enums;
using Code.Data.Items.Weapon;
using Code.Runtime.Modules.Statistics;

namespace Code.Runtime.Modules.Inventory
{
    public sealed class WeaponItem : TetrisItem, IWeaponItem
    {
        public MutableFloat          Damage                    { get; }
        public MutableFloat          AttackSpeed               { get; }
        public MutableFloat          ResourceCost              { get; }
        public MutableFloat          ResourceGenOnHit          { get; }
        public DeliveryPattern       Delivery                  { get; }
        public Affinity              Affinity                  { get; }
        public Anchor                Anchor                    { get; }
        public PayloadBehavior       Payload                   { get; }
        public WeaponItem(WeaponConfig config, RotationType rotation = RotationType.None) : base(config, rotation)
        {
            Damage                    = new MutableFloat(config.BaseDamage);
            AttackSpeed               = new MutableFloat(config.AttackSpeed);
            ResourceCost              = new MutableFloat(config.ResourceCost);
            ResourceGenOnHit          = new MutableFloat(config.ResourceGenOnHit);
            Delivery                  = config.Delivery;
            Affinity                  = config.Affinity;
            Anchor                    = config.Anchor;
            Payload                   = config.Payload;
        }
    }

    public interface IWeaponItem : ITetrisItem
    {
        //MutableInt           Range                     { get; }
        MutableFloat         Damage                    { get; }
        MutableFloat         AttackSpeed               { get; }
        MutableFloat         ResourceCost              { get; } // probably needs healthCost and manaCost separately
        MutableFloat         ResourceGenOnHit          { get; }
        DeliveryPattern      Delivery                  { get; }
        Affinity             Affinity                  { get; }
        Anchor               Anchor                    { get; }
        PayloadBehavior Payload { get; }
    }
}
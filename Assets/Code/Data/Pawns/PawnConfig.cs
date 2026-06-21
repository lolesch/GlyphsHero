using Code.Data.Items.Weapon;
using Submodules.Utility.Attributes;
using UnityEngine;

namespace Code.Data.Pawns
{
    [CreateAssetMenu(fileName = "PawnConfig", menuName = Const.ConfigRoot + "Pawns")]
    public sealed class PawnConfig : ScriptableObject
    {
        [PreviewIcon] public Sprite icon;
        [Min(1)] public uint baseHealth = 100;
        [Min(1)] public uint baseHealthRegen = 2;
        [Min(1)] public uint baseMana = 60;
        [Min(1)] public uint baseManaRegen = 5;
        [Min(0.1f)] public float movementSpeed = 1f;
        // Reach ceiling for range-scaling weapons (ADR-0001, Decision 2). 1 = brawler archetype;
        // raise per archetype for snipers. Capped + expensive by design — pricing owned by the
        // future balancing table, so this is authored per pawn, not pumped by items.
        [Min(1)] public uint baseRange = 1;

        public WeaponConfig starterWeapon;
        public TerrainCostConfig movementCosts;
        
        //public PawnEffectConfig pawnEffects;
    }
}

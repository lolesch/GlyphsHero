using System;
using System.Collections.Generic;
using Code.Data.Items;
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
        // Grid cell the starter weapon is placed at on spawn (TryAddAt, not auto-fit) so a scripted
        // encounter's starting topology — what's connected to the weapon vs. left loose — is
        // deterministic instead of depending on TryAdd's first-fit scan order.
        public Vector2Int starterWeaponPosition;
        public List<StarterItemPlacement> starterItems = new();
        public TerrainCostConfig movementCosts;

        //public PawnEffectConfig pawnEffects;

        [Serializable]
        public struct StarterItemPlacement
        {
            public ItemConfig config;
            public Vector2Int position;
        }
    }
}

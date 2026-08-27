using System;
using System.Collections.Generic;
using Code.Data.Items;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Code.Data.Pawns
{
    [CreateAssetMenu(fileName = "EncounterConfig", menuName = Const.ConfigRoot + "Encounters")]
    public sealed class EncounterConfig : ScriptableObject
    {
        // Full roster snapshot: PawnFactory.SpawnEnemies respawns from this list every time an
        // encounter loads, since enemies never persist across encounters.
        public List<SpawnData> enemies;
        // Delta, not a snapshot: only the pawn(s) newly introduced THIS encounter. Player pawns
        // are never cleared between encounters (their built inventory must persist), so re-listing
        // an already-fielded pawn here would spawn a duplicate, empty-inventory clone alongside it.
        public List<SpawnData> players;
        // Offered to the player when this encounter is won — not auto-granted. Scripted so early
        // encounters can double as a tutorial and later ones hand-tune the ramp.
        public List<ItemConfig> scriptedLoot = new();
        // How many of the offer above the player may take; the rest are discarded once spent or
        // once Continue is pressed. Not a currency/budget system — just how many picks this win buys.
        [Min(0)] public int lootPickCount = 1;

        [Serializable]
        public struct SpawnData
        {
            public PawnConfig config;
            public Hex startHex;
        }
    }
}
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
        // Granted to the stash when this encounter is won, in order. Replaces random pool loot —
        // scripted so early encounters can double as a tutorial and later ones hand-tune the ramp.
        public List<ItemConfig> scriptedLoot = new();

        [Serializable]
        public struct SpawnData
        {
            public PawnConfig config;
            public Hex startHex;
        }
    }
}
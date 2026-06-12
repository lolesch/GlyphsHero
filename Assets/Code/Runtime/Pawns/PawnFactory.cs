using Code.Data.Enums;
using Code.Data.Pawns;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Code.Runtime.Pawns
{
    public class PawnFactory : MonoBehaviour
    {
        [SerializeField] private Pawn pawnPrefab;
        [SerializeField] private Transform pawnParent;
        [SerializeField] private Grid grid;
        private PawnRegistry _registry;
        
        public void Initialize(PawnRegistry registry) => _registry = registry;

        public IPawn CreatePawn(PawnConfig config, Hex hex, PawnTeam team)
        {
            Pawn pawn = Instantiate(pawnPrefab, hex.ToWorld(grid), Quaternion.identity, pawnParent);
            pawn.SpawnPawn(config, team, hex, grid);
            
            _registry.Register(pawn);
            pawn.OnDefeated += () => { _registry.Unregister(pawn); };
            
            return pawn;
        }

        public void SpawnEnemies(EncounterConfig currentEncounter)
        {
            foreach (var data in currentEncounter.enemies)
                _ = CreatePawn(data.config, data.startHex, PawnTeam.Enemy);
        }
        
        public void SpawnAllys(EncounterConfig currentEncounter)
        {
            foreach (var data in currentEncounter.players)
                _ = CreatePawn(data.config, data.startHex, PawnTeam.Player);
        }
        
        public void SpawnAlly(PawnConfig config, Hex startHex)
        {
            _ = CreatePawn(config, startHex, PawnTeam.Player);
        }
    }
}
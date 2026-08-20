using Code.Data.Enums;
using Code.Data.Pawns;
using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Runtime.Pawns
{
    public class PawnFactory : MonoBehaviour
    {
        [SerializeField] private Pawn pawnPrefab;
        [SerializeField] private Transform pawnParent;
        [SerializeField] private Grid grid;
        // Scene singletons Draggable needs but a prefab asset can't carry — assigned onto each
        // spawned pawn's Draggable once, here, rather than have Draggable hunt for them at runtime.
        [SerializeField] private Camera cam;
        [SerializeField] private Tilemap tilemap;
        private PawnRegistry _registry;

        public void Initialize(PawnRegistry registry) => _registry = registry;

        public IPawn CreatePawn(PawnConfig config, Hex hex, PawnTeam team)
        {
            Pawn pawn = Instantiate(pawnPrefab, hex.ToWorld(grid), Quaternion.identity, pawnParent);
            pawn.SpawnPawn(config, team, hex, grid);

            var draggable = pawn.GetComponent<Draggable>();
            if (draggable != null)
                draggable.Initialize(cam, grid, tilemap);

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
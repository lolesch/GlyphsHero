using Code.Data.Enums;
using Code.Runtime.Modules.HexGrid;
using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Runtime.Pawns
{
    /// <summary>
    /// Handles pawn drag-and-drop onto the hex tilemap.
    /// Automatically disabled outside of PlacementPhase.
    /// </summary>
    public sealed class Draggable : MonoBehaviour
    {
        [SerializeField] private Camera      cam;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip   pickupClip, dropClip;
        [SerializeField] private Transform   pawn;
        [SerializeField] private Grid        grid;
        [SerializeField] private Tilemap     tilemap;
        [SerializeField] private Pawn        pawnActor;

        private bool    _isDragging;
        private Vector2 _offset;
        private Vector2 _previousPos;

        private void Awake()
        {
            _previousPos = pawn.position;
            if (pawnActor != null) return;
            pawnActor = pawn.GetComponent<Pawn>(); Debug.LogWarning("Assign _pawnActor in Inspector.", this);
        }

        void Start() => pawnActor.MoveTo(grid.WorldToCell(pawnActor.transform.position).CellToHex());

        // Grid/tilemap/camera are scene singletons and can't be baked into the prefab asset —
        // PawnFactory holds them (it already needs grid for spawn positioning) and assigns them
        // here right after Instantiate, once per spawned pawn.
        public void Initialize(Camera cam, Grid grid, Tilemap tilemap)
        {
            this.cam     = cam;
            this.grid    = grid;
            this.tilemap = tilemap;
        }

        
        // TODO: wire PhaseChange from GameLoop side. Pawn/Draggable should expose IPhaseListener OnPhaseChanged()
        public void OnPhaseChanged(GamePhase phase)
        {
            enabled = phase == GamePhase.Placement;
        }

        private void OnMouseDrag()
        {
            if (!_isDragging)
                return;

            pawn.position = GetMousePos() - _offset;
        }

        private void OnMouseDown()
        {
            _isDragging = true;
            audioSource.PlayOneShot(pickupClip);
            _offset = GetMousePos() - (Vector2)pawn.position;
        }

        private void OnMouseUp()
        {
            _isDragging = false;
            audioSource.PlayOneShot(dropClip);

            var cell = grid.WorldToCell(pawn.position);

            if (tilemap.HasTile(cell) && tilemap.GetTile(cell) is TerrainTileBase terrain)
            {
                pawn.position = grid.CellToWorld(cell);
                _previousPos  = pawn.position;
                pawnActor.MoveTo(cell.CellToHex());

                Debug.LogWarning($"Terrain: {terrain.type}");
            }
            else
                pawn.position = _previousPos;
        }

        private Vector2 GetMousePos() => cam.ScreenToWorldPoint(Input.mousePosition);
    }
}
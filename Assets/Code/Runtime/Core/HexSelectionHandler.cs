using System;
using System.Collections.Generic;
using System.Linq;
using Code.Runtime.Core;
using Code.Runtime.Modules.HexGrid;
using Code.Runtime.Pawns;
using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// Handles hex grid mouse selection.
    /// On hover: highlights the pawn's effect shape on the tilemap and refreshes the inventory view.
    /// No combat knowledge — purely selection and UI.
    /// </summary>
    public sealed class HexSelectionHandler : MonoBehaviour
    {
        [SerializeField] private Grid          grid;
        [SerializeField] private Tilemap       levelMap;
        [SerializeField] private Tilemap       pawnEffectMap;
        [SerializeField] private TileBase      effectTile;

        private PawnRegistry _registry;
        private Plane      _plane = new(Vector3.back, 0f);
        private Vector3Int _hoveredCell;
        private IPawn      hoveredPawn;
        private Camera     _cam;
        
        [SerializeField] private bool drawCellPos;
        [SerializeField] private bool drawHexPos;
        
        public static event Action<IPawn> OnPawnHovered;

        private static GUIStyle _centeredStyle;
        private static IEnumerable<Vector3Int> _allCells;
        
        void OnValidate() => _allCells = levelMap.GetAllCells();
        
        public void Initialize(PawnRegistry registry)
        {
            _registry = registry;
            _cam = Camera.main;
        }

        private void Update()
        {
            if (hoveredPawn != null)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                    hoveredPawn.PawnEffects.Rotate(false);
                if (Input.GetKeyDown(KeyCode.E))
                    hoveredPawn.PawnEffects.Rotate(true);
            }

            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (!_plane.Raycast(ray, out var distance))
                return;

            var cell = grid.WorldToCell(ray.GetPoint(distance));
            if (!levelMap.HasTile(cell) || _hoveredCell == cell)
                return;

            if (_hoveredCell != cell)
            {
                _hoveredCell = cell;
                //Debug.Log($"Hovered {_hoveredCell.CellToHex()}");
            }
            CheckHexForUnit();
        }

        private void CheckHexForUnit()
        {
            var pawn = _registry.allPawns.FirstOrDefault(x => x.HexPosition.ToCell() == _hoveredCell);
            hoveredPawn = pawn;
            
            if (pawn == default)
            {
                pawnEffectMap.ClearAllTiles();
                return;
            }
            
            //TODO: make this event based
            OnPawnHovered?.Invoke(pawn);
            //inventoryView.RefreshView(pawn);
            
            foreach (var hex in pawn.PawnEffects.GetHexes())
            {
                var cell = pawn.HexPosition.Add(hex).ToCell();
                pawnEffectMap.SetTile(cell, effectTile);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (grid == null) return;
            
            _allCells ??= levelMap.GetAllCells();
            
            if (_centeredStyle == null)
            {
                _centeredStyle = new GUIStyle(GUI.skin.label);
                _centeredStyle.alignment = TextAnchor.MiddleCenter;
                _centeredStyle.normal.textColor = Color.white;
            }
            
            foreach (var cell in _allCells)
            {
                var worldPos = grid.CellToWorld(cell);
                var hex = cell.CellToHex();
                
#if UNITY_EDITOR
                var offset = Vector3.zero;
                if (drawCellPos)
                {
                    if (drawHexPos) offset = Vector3.up * 0.1f;
                    UnityEditor.Handles.Label(worldPos + offset, $"Cell {(Vector2Int)cell}", _centeredStyle);
                }
                if (drawHexPos)
                {
                    if (drawCellPos) offset = Vector3.down * 0.1f;
                    UnityEditor.Handles.Label(worldPos + offset, $"{hex}", _centeredStyle);
                }
#endif
            }
        }
    }
}
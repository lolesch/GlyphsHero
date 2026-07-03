using System;
using System.Collections.Generic;
using System.Linq;
using Code.Data.Enums;
using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Runtime.Modules.HexGrid
{
    /// <summary>
    /// Scene-level spatial authority for the hex grid.
    /// Owns occupant registration, terrain lookup, and invalid-position queries.
    /// </summary>
    public sealed class HexGridController : MonoBehaviour, IHexGrid
    {
        [SerializeField] private Tilemap terrain;
        [SerializeField] private int levelIndex;
        
        //TODO: a lookup wrapper holding all terrain tiles
        [SerializeField] private TerrainTileBase expr;

        public TerrainType GetTerrain(Hex hex)
        {
            var cell = hex.ToCell();
            return terrain.GetTile(cell) is TerrainTileBase terrainTile
                ? terrainTile.type
                : TerrainType.Impassable;
        }

        public void SetTerrain(Hex hex, TerrainType type)
        {
            var tile = type switch
            {
                TerrainType.Dirt => expr,
                TerrainType.Sand => expr,
                TerrainType.Snow => expr,
                TerrainType.Impassable or
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            Debug.LogWarning($"[HexGrid] Set terrain at {hex} from {GetTerrain(hex)} to {tile.type}", this);
            terrain.SetTile(hex.ToCell(), tile);
        }

        [ContextMenu("SaveMap")]
        private void SaveMap()
        {
            var allTerrainTiles = terrain.GetAllCellTiles();
            
            var tiles = new List<TerrainTileData>();
            foreach (var tile in allTerrainTiles)
            {
                var test = new TerrainTileData
                {
                    x = tile.cell.x,
                    y = tile.cell.y,
                    t = tile.tile is TerrainTileBase t ? t.type : TerrainType.Impassable
                };
                tiles.Add(test);
            }
            var newTerrainData = new TerrainData
            {
                index = this.levelIndex,
                tiles = tiles,
            };
            var json = JsonUtility.ToJson( newTerrainData );
            
            var newTerrain = ScriptableObject.CreateInstance<TerrainConfig>();
            
            newTerrain.name = $"Terrain {levelIndex}";
            newTerrain.levelIndex = levelIndex;
            newTerrain.terrainTiles = allTerrainTiles.ToList();
            newTerrain.json = json;
            
            newTerrain.Save();
        }

        [ContextMenu("LoadMap")]
        private void LoadMap()
        {
            var map = Resources.Load<TerrainConfig>($"Terrain {levelIndex}");
            if (map == null)
            {
                Debug.LogError($"[HexGrid] Could not load terrain data for {levelIndex}");
                return;
            }

            terrain.ClearAllTiles();
            
            var loadedTerrain = JsonUtility.FromJson(map.json, typeof(TerrainData));
            
            foreach (var cellTile in map.terrainTiles)
                terrain.SetTile( cellTile.cell, cellTile.tile);
        }
    }
    
    public interface IHexGrid
    {
        /// <summary>Returns the terrain type at the given hex.</summary>
        TerrainType GetTerrain(Hex hex);
 
        /// <summary>Writes a terrain type onto the given hex tile.</summary>
        void SetTerrain(Hex hex, TerrainType type);
    }
}
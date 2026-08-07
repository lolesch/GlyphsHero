using System;
using System.Collections.Generic;
using Code.Data.Enums;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Code.Runtime.Modules.HexGrid
{
    [Serializable]
    public sealed class TerrainConfig : ScriptableObject
    {
        [SerializeField] public int levelIndex;
        [SerializeField] public List<CellTile> terrainTiles;
        [SerializeField, TextArea(10, 10)] public string json;
    }

    [Serializable]
    public struct TerrainData
    {
        [SerializeField] public int index;
        [SerializeField] public List<TerrainTileData> tiles;
    }

    [Serializable]
    public struct TerrainTileData
    {
        [SerializeField] public int x;
        [SerializeField] public int y;
        [SerializeField] public TerrainType t;
        
        //for optimization, we could store position as string x,y and the terrain type in the parent level,
        //having tiles sorted by type so the type is only saved once, followed by the array of positions
    }
}
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PieceData
{
    public Vector2Int position;
    public int pieceType;

    public PieceData(Vector2Int pos, int type)
    {
        position = pos;
        pieceType = type;
    }
}

[System.Serializable]
public class LevelData
{
    public int seed;
    public string levelName;
    public Vector2Int gridSize = new Vector2Int(4, 4);
    public List<Vector2Int> blockerPositions;
    public List<PieceData> initialPiecePositions;

    public LevelData()
    {
        blockerPositions = new();
        initialPiecePositions = new();
    }
}
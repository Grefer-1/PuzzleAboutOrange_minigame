using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class LevelGenerator : MonoBehaviour
{
    public int gridWidth = 4;
    public int gridHeight = 4;
    public int minBlockers = 3;
    public int maxBlockers = 4;

    private HashSet<int> usedSeedsThisSession_Generator = new();
    private System.Random randomForSeedGeneration = new();

    public int RequestNewUnusedSeed()
    {
        int newSeedValue;
        int attemptCount = 0;
        const int maxSeedAttempts = 1000;

        do
        {
            newSeedValue = randomForSeedGeneration.Next(int.MinValue, int.MaxValue);
            attemptCount = attemptCount + 1;

            if (attemptCount > maxSeedAttempts)
                break;
        }
        while (usedSeedsThisSession_Generator.Contains(newSeedValue));

        usedSeedsThisSession_Generator.Add(newSeedValue);
        return newSeedValue;
    }

    public void MarkSeedAsUsed(int seedToMark)
    {
        usedSeedsThisSession_Generator.Add(seedToMark);
    }

    private struct GameState : System.IEquatable<GameState>
    {
        public readonly Vector2Int[] piecePositions;

        public GameState(List<PieceData> piecesList)
        {
            List<PieceData> orderedPiecesList = new();
            if (piecesList != null)
            {
                List<PieceData> tempList = new(piecesList);
                tempList.Sort((p1, p2) => p1.pieceType.CompareTo(p2.pieceType));
                orderedPiecesList = tempList;
            }


            if (orderedPiecesList.Count != 4)
                throw new System.ArgumentException("GameState requires exactly 4 pieces to be initialized.");

            piecePositions = new Vector2Int[orderedPiecesList.Count];

            for (int i = 0; i < orderedPiecesList.Count; i = i + 1)
                piecePositions[i] = orderedPiecesList[i].position;
        }

        public GameState(Vector2Int[] positionsArray)
        {
            piecePositions = new Vector2Int[positionsArray.Length];
            System.Array.Copy(positionsArray, piecePositions, positionsArray.Length);
        }

        public bool Equals(GameState otherState)
        {
            if (piecePositions.Length != otherState.piecePositions.Length)
                return false;

            for (int i = 0; i < piecePositions.Length; i = i + 1)
            {
                if (piecePositions[i].x != otherState.piecePositions[i].x || piecePositions[i].y != otherState.piecePositions[i].y)
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int calculatedHashCode = 17;

            for (int i = 0; i < this.piecePositions.Length; i = i + 1)
                calculatedHashCode = calculatedHashCode * 31 + this.piecePositions[i].GetHashCode();

            return calculatedHashCode;
        }

        public override string ToString()
        {
            if (piecePositions == null || piecePositions.Length == 0)
                return "EmptyState";

            string s = "";

            for (int i = 0; i < piecePositions.Length; i++)
            {
                s += piecePositions[i].ToString();

                if (i < piecePositions.Length - 1)
                    s += ", ";
            }

            return s;
        }
    }


    private bool CheckWinConditionInState(GameState currentState, int currentGridWidth, int currentGridHeight)
    {
        if (currentState.piecePositions == null || currentState.piecePositions.Length != 4)
            return false;

        List<PieceData> piecesInCurrentState = new();

        for (int i = 0; i < currentState.piecePositions.Length; i = i + 1)
            piecesInCurrentState.Add(new PieceData(currentState.piecePositions[i], i));

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        for (int i = 0; i < piecesInCurrentState.Count; i = i + 1)
        {
            PieceData piece = piecesInCurrentState[i];

            if (piece.position.x < minX)
                minX = piece.position.x;
            if (piece.position.x > maxX)
                maxX = piece.position.x;
            if (piece.position.y < minY)
                minY = piece.position.y;
            if (piece.position.y > maxY)
                maxY = piece.position.y;
        }

        bool isTwoByTwoSquare = (maxX - minX == 1) && (maxY - minY == 1);

        if (isTwoByTwoSquare)
        {
            bool foundTopLeft = false;
            bool foundTopRight = false;
            bool foundBottomLeft = false;
            bool foundBottomRight = false;

            for (int pieceTypeIndex = 0; pieceTypeIndex < 4; pieceTypeIndex = pieceTypeIndex + 1)
            {
                Vector2Int pieceAbsolutePos = currentState.piecePositions[pieceTypeIndex];

                if (pieceAbsolutePos.x >= minX && pieceAbsolutePos.x <= maxX && pieceAbsolutePos.y >= minY && pieceAbsolutePos.y <= maxY)
                {
                    Vector2Int relativePosInSquare = pieceAbsolutePos - new Vector2Int(minX, minY);

                    if (relativePosInSquare.x == 0 && relativePosInSquare.y == 1 && pieceTypeIndex == 0)
                        foundTopLeft = true;
                    else if (relativePosInSquare.x == 1 && relativePosInSquare.y == 1 && pieceTypeIndex == 1)
                        foundTopRight = true;
                    else if (relativePosInSquare.x == 0 && relativePosInSquare.y == 0 && pieceTypeIndex == 2)
                        foundBottomLeft = true;
                    else if (relativePosInSquare.x == 1 && relativePosInSquare.y == 0 && pieceTypeIndex == 3)
                        foundBottomRight = true;
                }
            }
            return foundTopLeft && foundTopRight && foundBottomLeft && foundBottomRight;
        }
        return false;
    }

    private int GetShortestSolutionLength(LevelData levelData)
    {
        GameState initialGameState = new(levelData.initialPiecePositions);

        Queue<Tuple<GameState, int>> statesToVisit = new();
        HashSet<GameState> visitedStates = new();

        statesToVisit.Enqueue(Tuple.Create(initialGameState, 0));
        visitedStates.Add(initialGameState);

        int maxBfsSearchIterations = 75000;
        int currentIterationCount = 0; 

        Vector2Int[] swipeDirections = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (statesToVisit.Count > 0)
        {
            currentIterationCount = currentIterationCount + 1;

            if (currentIterationCount > maxBfsSearchIterations)
                return -1;

            Tuple<GameState, int> currentQueueEntry = statesToVisit.Dequeue();
            GameState currentGameState = currentQueueEntry.Item1;
            int currentPathLen = currentQueueEntry.Item2;

            if (CheckWinConditionInState(currentGameState, this.gridWidth, this.gridHeight))
                return currentPathLen;

            for (int dirIndex = 0; dirIndex < swipeDirections.Length; dirIndex = dirIndex + 1)
            {
                Vector2Int currentDirection = swipeDirections[dirIndex];

                Vector2Int[] nextPiecePositions = new Vector2Int[currentGameState.piecePositions.Length];
                System.Array.Copy(currentGameState.piecePositions, nextPiecePositions, currentGameState.piecePositions.Length);

                for (int i = 0; i < nextPiecePositions.Length; i = i + 1)
                {
                    Vector2Int pieceCurrentPos = currentGameState.piecePositions[i];
                    Vector2Int piecePotentialNewPos = pieceCurrentPos + currentDirection;

                    bool isOutOfBounds = piecePotentialNewPos.x < 0 || piecePotentialNewPos.x >= this.gridWidth || piecePotentialNewPos.y < 0 || piecePotentialNewPos.y >= this.gridHeight;
                    bool hitsBlocker = levelData.blockerPositions.Contains(piecePotentialNewPos);

                    if (isOutOfBounds || hitsBlocker)
                        nextPiecePositions[i] = pieceCurrentPos;
                    else
                        nextPiecePositions[i] = piecePotentialNewPos;
                }

                bool collisionResolvedInPass;
                int resolutionPassCount = 0;

                do
                {
                    collisionResolvedInPass = false;
                    resolutionPassCount = resolutionPassCount + 1;
                    Dictionary<Vector2Int, List<int>> pieceIndicesAtLandingSpots = new();

                    for (int i = 0; i < nextPiecePositions.Length; i = i + 1)
                    {
                        Vector2Int intendedLandingPos = nextPiecePositions[i];

                        if (!pieceIndicesAtLandingSpots.ContainsKey(intendedLandingPos))
                            pieceIndicesAtLandingSpots[intendedLandingPos] = new();

                        pieceIndicesAtLandingSpots[intendedLandingPos].Add(i);
                    }

                    for (int i = 0; i < nextPiecePositions.Length; i = i + 1)
                    {
                        Vector2Int originalPosOfThisPiece = currentGameState.piecePositions[i];
                        Vector2Int intendedTargetOfThisPiece = nextPiecePositions[i];

                        if (intendedTargetOfThisPiece.Equals(originalPosOfThisPiece))
                            continue;

                        if (pieceIndicesAtLandingSpots.ContainsKey(intendedTargetOfThisPiece) && pieceIndicesAtLandingSpots[intendedTargetOfThisPiece].Count > 1)
                        {
                            List<int> conflictingPieceIndices = pieceIndicesAtLandingSpots[intendedTargetOfThisPiece];

                            for (int k = 0; k < conflictingPieceIndices.Count; k++)
                            {
                                int pieceIdxToRevert = conflictingPieceIndices[k];

                                if (!nextPiecePositions[pieceIdxToRevert].Equals(currentGameState.piecePositions[pieceIdxToRevert]))
                                {
                                    nextPiecePositions[pieceIdxToRevert] = currentGameState.piecePositions[pieceIdxToRevert];
                                    collisionResolvedInPass = true;
                                }
                            }
                        }
                    }
                }
                while (collisionResolvedInPass && resolutionPassCount < 10);

                GameState nextGeneratedState = new(nextPiecePositions);

                if (!visitedStates.Contains(nextGeneratedState))
                {
                    visitedStates.Add(nextGeneratedState);
                    statesToVisit.Enqueue(Tuple.Create(nextGeneratedState, currentPathLen + 1));
                }
            }
        }
        return -1;
    }

    public LevelData GenerateLevel(int inputSeed, string levelName, int minSolutionPathTarget = -1, int maxSolutionPathTarget = -1)
    {
        int placementAttemptCount = 0;
        const int maxPlacementAttemptsPerSeed = 150;

        for (int currentAttempt = 0; currentAttempt < maxPlacementAttemptsPerSeed; currentAttempt = currentAttempt + 1)
        {
            placementAttemptCount = placementAttemptCount + 1;
            UnityEngine.Random.InitState(inputSeed + currentAttempt);

            LevelData newLevelData = new LevelData();
            newLevelData.seed = inputSeed;
            newLevelData.levelName = levelName;
            newLevelData.gridSize = new Vector2Int(gridWidth, gridHeight);
            newLevelData.blockerPositions.Clear();

            List<Vector2Int> availableGridCells = new();

            for (int x = 0; x < gridWidth; x = x + 1)
            {
                for (int y = 0; y < gridHeight; y = y + 1)
                    availableGridCells.Add(new Vector2Int(x, y));
            }

            int numberOfBlockers = UnityEngine.Random.Range(minBlockers, maxBlockers + 1);

            for (int i = 0; i < numberOfBlockers; i = i + 1)
            {
                if (availableGridCells.Count == 0)
                    break;

                int randomIndex = UnityEngine.Random.Range(0, availableGridCells.Count);
                newLevelData.blockerPositions.Add(availableGridCells[randomIndex]);
                availableGridCells.RemoveAt(randomIndex);
            }

            newLevelData.initialPiecePositions.Clear();
            List<int> pieceTypesToPlace = new() { 0, 1, 2, 3 };

            for (int i = 0; i < pieceTypesToPlace.Count - 1; i = i + 1)
            {
                int randomIndexToSwap = UnityEngine.Random.Range(i, pieceTypesToPlace.Count);
                int temp = pieceTypesToPlace[i];
                pieceTypesToPlace[i] = pieceTypesToPlace[randomIndexToSwap];
                pieceTypesToPlace[randomIndexToSwap] = temp;
            }

            for (int i = 0; i < 4; i = i + 1)
            {
                if (availableGridCells.Count == 0)
                    goto nextPlacementAttemptLabel;

                int randomIndex = UnityEngine.Random.Range(0, availableGridCells.Count);
                Vector2Int piecePos = availableGridCells[randomIndex];
                availableGridCells.RemoveAt(randomIndex);
                newLevelData.initialPiecePositions.Add(new(piecePos, pieceTypesToPlace[i]));
            }

            newLevelData.initialPiecePositions.Sort((p1, p2) => p1.pieceType.CompareTo(p2.pieceType));
            int calculatedSolutionLength = GetShortestSolutionLength(newLevelData);

            if (calculatedSolutionLength != -1)
            {
                bool meetsPathTarget = (minSolutionPathTarget == -1) || (calculatedSolutionLength >= minSolutionPathTarget && calculatedSolutionLength <= maxSolutionPathTarget);

                if (meetsPathTarget)
                    return newLevelData;
            }

        nextPlacementAttemptLabel:;
        }

        return null;
    }

    public void SaveLevelToJson(LevelData levelDataToSave, string filePath)
    {
        string jsonString = JsonUtility.ToJson(levelDataToSave, true);
        File.WriteAllText(filePath, jsonString);
    }
}
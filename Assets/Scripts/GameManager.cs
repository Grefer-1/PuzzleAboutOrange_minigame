using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Grid Setup")]
    public RectTransform gridPanelRect;
    public GameObject cellPrefab;
    public GameObject blockerPrefab;
    public GameObject movablePiecePrefab;

    public int gridWidth = 4;
    public int gridHeight = 4;

    public Vector2 cellSize;
    public Vector2 cellSpacing;
    public RectOffset padding;

    [Header("Piece Sprites")]
    public Sprite pieceSpriteTL;
    public Sprite pieceSpriteTR;
    public Sprite pieceSpriteBL;
    public Sprite pieceSpriteBR;

    [Header("Level Loading (Gameplay)")]
    public string levelsResourceSubFolder = "Levels";

    [Header("Timer & Rating")]
    public TextMeshProUGUI timerText;
    public float levelTimeLimit = 45f;
    private float currentTime;
    public bool timerIsRunning = false;
    public float threeStarTimeThreshold = 30f;
    public float twoStarTimeThreshold = 15f;

    [Header("Game State")]
    private LevelData currentLevelData;
    private List<Movable> movablePiecesList;
    private HashSet<Vector2Int> blockerPositionsSet;
    private Dictionary<Vector2Int, Cell> gridCellsDictionary;
    public bool isMoving = false;
    public float moveSpeed = 5f;
    private string currentPlayingLevelFileName;

    [Header("Gameplay UI Buttons")]
    public Button gameplayHomeButton;
    public Button gameplayRetryButton;


    void Awake()
    {
        Instance = this;

        movablePiecesList = new();
        blockerPositionsSet = new();
        gridCellsDictionary = new();
    }

    void OnEnable()
    {
        PlayerInput.OnSwipeDetected += HandlePlayerMoveInput;
    }

    void OnDisable()
    {
        PlayerInput.OnSwipeDetected -= HandlePlayerMoveInput;
    }

    void Update()
    {
        if (timerIsRunning)
        {
            currentTime -= Time.deltaTime;

            if (timerText != null)
            {
                float displayTime = currentTime;

                if (displayTime < 0)
                    displayTime = 0;

                timerText.text = displayTime.ToString("F1");
            }

            if (currentTime <= 0)
            {
                currentTime = 0;
                timerIsRunning = false;
                HandleLevelFail_TimeUp();
            }
        }
    }

    void InitializeLevelState()
    {
        currentTime = levelTimeLimit;

        if (timerText != null)
            timerText.text = currentTime.ToString("F1");

        isMoving = false;
        timerIsRunning = false;
    }

    public void StartOrRestartLevel(string levelFileNameKey)
    {
        LevelSelectionManager.EnsureLevelOrderInitialized();
        currentPlayingLevelFileName = levelFileNameKey;
        string resourcePathForLevel = Path.Combine(levelsResourceSubFolder, currentPlayingLevelFileName);
        TextAsset levelAsset = Resources.Load<TextAsset>(resourcePathForLevel);

        if (UIManager.Instance != null && UIManager.Instance.howToPlayCanvas != null)
            UIManager.Instance.ShowHowToPlay(false);

        if (levelAsset != null)
        {
            currentLevelData = JsonUtility.FromJson<LevelData>(levelAsset.text);
            if (currentLevelData != null)
            {
                SetupGridAndPiecesFromData();
                InitializeLevelState();

                bool isFirstLevel = false;

                if (LevelSelectionManager.SortedLevelFileNames.Count > 0)
                    isFirstLevel = currentPlayingLevelFileName.Equals(LevelSelectionManager.SortedLevelFileNames[0]);

                if (isFirstLevel)
                    ShowHowToPlayPopup();
                else
                {
                    timerIsRunning = true;
                    isMoving = false;
                }
            }
        }
    }

    void ShowHowToPlayPopup()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowHowToPlay(true);
            timerIsRunning = false;
            isMoving = true;

            if (timerText != null)
                timerText.gameObject.SetActive(false);
        }
    }

    public void OnHowToPlayClosed()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowHowToPlay(false);

        bool gameHasAlreadyEnded = false;

        if (UIManager.Instance != null && UIManager.Instance.resultCanvas != null && UIManager.Instance.resultCanvas.activeSelf)
            gameHasAlreadyEnded = true;

        if (currentLevelData != null && !gameHasAlreadyEnded)
        {
            isMoving = false;
            timerIsRunning = true;

            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
                float displayTime = currentTime;

                if (displayTime < 0)
                    displayTime = 0;

                timerText.text = displayTime.ToString("F1");
            }

            if (currentTime <= 0 && timerIsRunning)
            {
                timerIsRunning = false;
                HandleLevelFail_TimeUp();
            }
        }
    }

    private Vector2Int LogicalToCellGridPosition(Vector2Int logicalPos)
    {
        return new Vector2Int(logicalPos.x, (gridHeight - 1) - logicalPos.y);
    }

    private Vector2 CalculateCellCenterAnchoredPosition(int cell_x, int cell_y)
    {
        float xPos = padding.left + cell_x * (cellSize.x + cellSpacing.x) + (cellSize.x * 0.5f);
        float yPos = -padding.top - cell_y * (cellSize.y + cellSpacing.y) - (cellSize.y * 0.5f);
        return new Vector2(xPos, yPos);
    }

    void SetupGridAndPiecesFromData()
    {
        ClearGridBeforeLoad();

        gridWidth = currentLevelData.gridSize.x;
        gridHeight = currentLevelData.gridSize.y;

        if (cellPrefab != null)
        {
            for (int cell_y = 0; cell_y < gridHeight; cell_y++)
            {
                for (int cell_x = 0; cell_x < gridWidth; cell_x++)
                {
                    GameObject cellGO = Instantiate(cellPrefab, gridPanelRect);
                    cellGO.name = "Cell_M_" + cell_x + "_" + cell_y;

                    RectTransform cellRect = cellGO.GetComponent<RectTransform>();
                    cellRect.anchorMin = new Vector2(0, 1);
                    cellRect.anchorMax = new Vector2(0, 1);
                    cellRect.pivot = new Vector2(0.5f, 0.5f);
                    cellRect.sizeDelta = cellSize;
                    cellRect.anchoredPosition = CalculateCellCenterAnchoredPosition(cell_x, cell_y);

                    Cell cellScript = cellGO.GetComponent<Cell>();

                    if (cellScript == null)
                        cellScript = cellGO.AddComponent<Cell>();

                    Vector2Int currentCellGridPos = new Vector2Int(cell_x, cell_y);
                    cellScript.gridPosition = currentCellGridPos;
                    gridCellsDictionary[currentCellGridPos] = cellScript;
                }
            }
        }
        else
            return;

        if (currentLevelData.blockerPositions != null)
        {
            foreach (Vector2Int logicalBlockerPos in currentLevelData.blockerPositions)
            {
                blockerPositionsSet.Add(logicalBlockerPos);
                Vector2Int cellPosForBlocker = LogicalToCellGridPosition(logicalBlockerPos);
                if (gridCellsDictionary.ContainsKey(cellPosForBlocker))
                {
                    Cell targetCell = gridCellsDictionary[cellPosForBlocker];
                    GameObject blockerGO = Instantiate(blockerPrefab, targetCell.transform);
                    RectTransform blockerRect = blockerGO.GetComponent<RectTransform>();
                    blockerRect.anchorMin = Vector2.zero; blockerRect.anchorMax = Vector2.one;
                    blockerRect.pivot = new Vector2(0.5f, 0.5f);
                    blockerRect.offsetMin = Vector2.zero; blockerRect.offsetMax = Vector2.zero;
                    targetCell.occupantBlocker = blockerGO;
                }
            }
        }

        if (currentLevelData.initialPiecePositions != null)
        {
            List<PieceData> sortedInitialPieces = new List<PieceData>(currentLevelData.initialPiecePositions);
            sortedInitialPieces.Sort((p1, p2) => p1.pieceType.CompareTo(p2.pieceType));

            foreach (PieceData pieceData in sortedInitialPieces)
            {
                Vector2Int cellPosForPiece = LogicalToCellGridPosition(pieceData.position);
                if (gridCellsDictionary.ContainsKey(cellPosForPiece))
                {
                    Cell targetCell = gridCellsDictionary[cellPosForPiece];
                    GameObject pieceGO = Instantiate(movablePiecePrefab, targetCell.transform);
                    Movable pieceUI = pieceGO.GetComponent<Movable>();
                    Sprite pieceSprite = GetSpriteForPieceType(pieceData.pieceType);
                    pieceUI.Initialize(new PieceData(pieceData.position, pieceData.pieceType), pieceSprite);

                    RectTransform pieceRect = pieceUI.rectTransform;
                    pieceRect.anchorMin = Vector2.zero; pieceRect.anchorMax = Vector2.one;
                    pieceRect.pivot = new Vector2(0.5f, 0.5f);
                    pieceRect.offsetMin = Vector2.zero; pieceRect.offsetMax = Vector2.zero;

                    movablePiecesList.Add(pieceUI);
                    targetCell.occupantPiece = pieceUI;
                }
            }
        }

        movablePiecesList.Sort((m1, m2) => m1.pieceData.pieceType.CompareTo(m2.pieceData.pieceType));
    }

    Sprite GetSpriteForPieceType(int type)
    {
        if (type == 0) return pieceSpriteTL;
        if (type == 1) return pieceSpriteTR;
        if (type == 2) return pieceSpriteBL;
        if (type == 3) return pieceSpriteBR;
        return null;
    }

    void ClearGridBeforeLoad()
    {
        for (int i = gridPanelRect.childCount - 1; i >= 0; i--)
        {
            Transform child = gridPanelRect.GetChild(i);

            if (child.GetComponent<Cell>() != null)
                Destroy(child.gameObject);
        }

        gridCellsDictionary.Clear();
        movablePiecesList.Clear();
        blockerPositionsSet.Clear();
    }

    public void HandlePlayerMoveInput(Vector2Int direction)
    {
        if (isMoving || !timerIsRunning || currentLevelData == null)
            return;

        Dictionary<Movable, Vector2Int> intendedNextPositions = new Dictionary<Movable, Vector2Int>();

        for (int i = 0; i < movablePiecesList.Count; i++)
        {
            Movable piece = movablePiecesList[i];
            Vector2Int currentPos = piece.pieceData.position;
            Vector2Int potentialNextPos = currentPos + direction;

            bool canMove = true;
            if (potentialNextPos.x < 0 || potentialNextPos.x >= gridWidth || potentialNextPos.y < 0 || potentialNextPos.y >= gridHeight || blockerPositionsSet.Contains(potentialNextPos))
                canMove = false;

            if (canMove)
                intendedNextPositions[piece] = potentialNextPos;
            else
                intendedNextPositions[piece] = currentPos;
        }

        bool changedThisPass;
        int safetyCounter = 0;

        do
        {
            changedThisPass = false;
            safetyCounter++;
            Dictionary<Vector2Int, List<Movable>> landingSpots = new Dictionary<Vector2Int, List<Movable>>();

            for (int i = 0; i < movablePiecesList.Count; i++)
            {
                Movable piece = movablePiecesList[i];
                Vector2Int intendedPos = intendedNextPositions[piece];

                if (!landingSpots.ContainsKey(intendedPos))
                    landingSpots[intendedPos] = new List<Movable>();

                landingSpots[intendedPos].Add(piece);
            }

            for (int i = 0; i < movablePiecesList.Count; i++)
            {
                Movable piece = movablePiecesList[i];
                Vector2Int originalPos = piece.pieceData.position;
                Vector2Int intendedTargetPos = intendedNextPositions[piece];

                if (intendedTargetPos.Equals(originalPos))
                    continue;

                if (landingSpots.ContainsKey(intendedTargetPos))
                {
                    if (landingSpots[intendedTargetPos].Count > 1)
                    {
                        List<Movable> conflictingPieces = landingSpots[intendedTargetPos];

                        for (int j = 0; j < conflictingPieces.Count; j++)
                        {
                            Movable conflictPiece = conflictingPieces[j];

                            if (!intendedNextPositions[conflictPiece].Equals(conflictPiece.pieceData.position))
                            {
                                intendedNextPositions[conflictPiece] = conflictPiece.pieceData.position;
                                changedThisPass = true;
                            }
                        }
                    }
                }
            }
        }
        while (changedThisPass && safetyCounter < 10);

        StartCoroutine(MovePiecesCoroutine(intendedNextPositions));
    }

    IEnumerator MovePiecesCoroutine(Dictionary<Movable, Vector2Int> newPiecePositions)
    {
        isMoving = true;
        float timer = 0f;
        float duration = 1.0f / moveSpeed;

        if (duration <= 0.001f)
            duration = 0.2f;

        Dictionary<Movable, Vector3> startPositionsWorld = new();
        Dictionary<Movable, Vector3> endPositionsWorld = new();
        Dictionary<Movable, Transform> originalParentsDict = new();

        bool anyMoved = false;

        foreach (KeyValuePair<Movable, Vector2Int> entry in newPiecePositions)
        {
            Movable piece = entry.Key;
            Vector2Int targetPosLogical = entry.Value;

            if (!piece.pieceData.position.Equals(targetPosLogical))
            {
                anyMoved = true;
                originalParentsDict[piece] = piece.transform.parent;
                startPositionsWorld[piece] = piece.rectTransform.position;

                Vector2Int targetCellVisual = LogicalToCellGridPosition(targetPosLogical);
                if (gridCellsDictionary.ContainsKey(targetCellVisual))
                    endPositionsWorld[piece] = gridCellsDictionary[targetCellVisual].rectTransform.position;
                else
                    endPositionsWorld[piece] = startPositionsWorld[piece];

                piece.transform.SetParent(gridPanelRect, true);
            }
        }

        if (!anyMoved)
        {
            isMoving = false;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float percent = timer / duration;
            if (percent > 1f) percent = 1f;

            foreach (Movable pieceToMove in startPositionsWorld.Keys)
                pieceToMove.rectTransform.position = Vector3.Lerp(startPositionsWorld[pieceToMove], endPositionsWorld[pieceToMove], percent);

            yield return null;
        }

        foreach (KeyValuePair<Movable, Vector2Int> entry in newPiecePositions)
        {
            Movable piece = entry.Key;
            Vector2Int finalPosLogical = entry.Value;

            bool didMoveLogically = !piece.pieceData.position.Equals(finalPosLogical);

            if (didMoveLogically)
            {
                if (originalParentsDict.ContainsKey(piece))
                {
                    Transform oldParentTransform = originalParentsDict[piece];
                    if (oldParentTransform != null)
                    {
                        Cell oldCell = oldParentTransform.GetComponent<Cell>();

                        if (oldCell != null && oldCell.occupantPiece == piece)
                            oldCell.occupantPiece = null;
                    }
                }
            }

            piece.UpdateDataPosition(finalPosLogical);

            Vector2Int finalCellVisual = LogicalToCellGridPosition(finalPosLogical);
            if (gridCellsDictionary.ContainsKey(finalCellVisual))
            {
                Cell targetCell = gridCellsDictionary[finalCellVisual];
                piece.transform.SetParent(targetCell.transform, false);

                RectTransform pieceRect = piece.rectTransform;
                pieceRect.anchorMin = Vector2.zero;
                pieceRect.anchorMax = Vector2.one;
                pieceRect.pivot = new Vector2(0.5f, 0.5f);
                pieceRect.offsetMin = Vector2.zero;
                pieceRect.offsetMax = Vector2.zero;
                targetCell.occupantPiece = piece;
            }
            else
            {
                if (originalParentsDict.ContainsKey(piece) && originalParentsDict[piece] != null)
                    piece.transform.SetParent(originalParentsDict[piece], false);
                else
                    piece.transform.SetParent(gridPanelRect, false);

                RectTransform pieceRect = piece.rectTransform;
                pieceRect.anchorMin = Vector2.zero; pieceRect.anchorMax = Vector2.one;
                pieceRect.pivot = new Vector2(0.5f, 0.5f);
                pieceRect.offsetMin = Vector2.zero; pieceRect.offsetMax = Vector2.zero;
            }
        }

        isMoving = false;

        if (timerIsRunning)
            CheckWinCondition();
    }

    void CheckWinCondition()
    {
        if (movablePiecesList.Count != 4)
            return;

        List<PieceData> currentPieces = new List<PieceData>();

        for (int i = 0; i < movablePiecesList.Count; i++)
            currentPieces.Add(movablePiecesList[i].pieceData);

        currentPieces.Sort((p1, p2) =>
        {
            int xCompare = p1.position.x.CompareTo(p2.position.x);

            if (xCompare == 0)
                return p1.position.y.CompareTo(p2.position.y);

            return xCompare;
        });


        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;

        for (int i = 0; i < currentPieces.Count; i++)
        {
            PieceData p = currentPieces[i];
            if (p.position.x < minX) minX = p.position.x;
            if (p.position.x > maxX) maxX = p.position.x;
            if (p.position.y < minY) minY = p.position.y;
            if (p.position.y > maxY) maxY = p.position.y;
        }

        if ((maxX - minX == 1) && (maxY - minY == 1))
        {
            bool foundTL = false, foundTR = false, foundBL = false, foundBR = false;

            for (int i = 0; i < currentPieces.Count; i++)
            {
                PieceData p_checkType = currentPieces[i];
                p_checkType = movablePiecesList.Find(mp => mp.pieceData.position == currentPieces[i].position && mp.pieceData.pieceType == currentPieces[i].pieceType).pieceData; // More robust find

                Vector2Int relativePos = p_checkType.position - new Vector2Int(minX, minY);
                if (relativePos.Equals(new Vector2Int(0, 1)) && p_checkType.pieceType == 0) foundTL = true;
                else if (relativePos.Equals(new Vector2Int(1, 1)) && p_checkType.pieceType == 1) foundTR = true;
                else if (relativePos.Equals(new Vector2Int(0, 0)) && p_checkType.pieceType == 2) foundBL = true;
                else if (relativePos.Equals(new Vector2Int(1, 0)) && p_checkType.pieceType == 3) foundBR = true;
            }

            if (foundTL && foundTR && foundBL && foundBR)
            {
                HandleLevelWin();
                return;
            }
        }
    }

    void HandleLevelWin()
    {
        if (isMoving && !timerIsRunning && UIManager.Instance?.resultCanvas?.activeSelf == true && UIManager.Instance.resultCanvas.GetComponent<Result>()?.loseImage?.gameObject.activeSelf == true)
            return;

        timerIsRunning = false;
        isMoving = true;
        int stars = CalculateStars(currentTime);
        LevelProgressionManager.SetStarRating(currentPlayingLevelFileName, stars);
        string nextLevelNameFromManager = LevelSelectionManager.GetNextLevelInSequence(currentPlayingLevelFileName);

        if (!string.IsNullOrEmpty(nextLevelNameFromManager))
            LevelProgressionManager.UnlockLevel(nextLevelNameFromManager);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowResultScreen();
            Result rs = UIManager.Instance.resultCanvas?.GetComponent<Result>();

            if (rs != null)
                rs.SetupResult(true, stars, currentTime, currentPlayingLevelFileName, nextLevelNameFromManager);
        }
    }

    void HandleLevelFail_TimeUp()
    {
        if (isMoving && !timerIsRunning)
        {
            if (UIManager.Instance != null && UIManager.Instance.resultCanvas != null && UIManager.Instance.resultCanvas.activeSelf)
            {
                Result res = UIManager.Instance.resultCanvas.GetComponent<Result>();

                if (res != null && res.winImage != null && res.winImage.gameObject.activeSelf)
                    return;
            }
        }

        isMoving = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowResultScreen();
            Result resultScreen = null;

            if (UIManager.Instance.resultCanvas != null)
                resultScreen = UIManager.Instance.resultCanvas.GetComponent<Result>();

            if (resultScreen != null)
                resultScreen.SetupResult(false, 0, 0, currentPlayingLevelFileName, "");
        }

        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }

    int CalculateStars(float remainingTime)
    {
        if (remainingTime >= threeStarTimeThreshold) return 3;
        if (remainingTime >= twoStarTimeThreshold) return 2;
        if (remainingTime > 0.001f) return 1;
        return 0;
    }

    public void OnGameplayHomeButtonPressed()
    {
        timerIsRunning = false;
        isMoving = true;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowMainMenu();
    }

    public void OnGameplayRetryButtonPressed()
    {
        timerIsRunning = false;
        isMoving = true;

        if (!string.IsNullOrEmpty(currentPlayingLevelFileName))
            StartOrRestartLevel(currentPlayingLevelFileName);
        else
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowMainMenu();
        }
    }
}
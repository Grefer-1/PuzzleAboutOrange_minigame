using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvases")]
    public GameObject mainMenuCanvas;
    public GameObject levelSelectionCanvas;
    public GameObject howToPlayCanvas;
    public GameObject gameplayCanvas;
    public GameObject resultCanvas;
    public LevelSelectionManager levelSelectionManager;

    private GameObject currentActiveCanvas;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ShowMainMenu();
    }

    private void ShowCanvas(GameObject canvasToShow)
    {
        if (currentActiveCanvas != null && currentActiveCanvas != canvasToShow)
            currentActiveCanvas.SetActive(false);

        if (canvasToShow != null)
        {
            canvasToShow.SetActive(true);
            currentActiveCanvas = canvasToShow;

            if (canvasToShow == levelSelectionCanvas && levelSelectionManager != null)
                levelSelectionManager.PopulateLevels();
        }
    }

    public void ShowMainMenu()
    {
        ShowCanvas(mainMenuCanvas);
    }

    public void ShowLevelSelection()
    {
        ShowCanvas(levelSelectionCanvas);
    }

    public void ShowGameplay()
    {
        ShowCanvas(gameplayCanvas);
    }

    public void ShowHowToPlay(bool show)
    {
        if (howToPlayCanvas != null)
            howToPlayCanvas.SetActive(show);
    }

    public void ShowResultScreen()
    {
        ShowCanvas(resultCanvas);
    }

    public void OnPlayButtonPressed()
    {
        ShowLevelSelection();
    }

    public void OnBackToMainMenuPressed()
    {
        ShowMainMenu();
    }

    public void OnQuitButtonPressed()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Result : MonoBehaviour
{
    [Header("Result Display")]
    public Image winImage;
    public Image loseImage;

    [Header("Stars Display")]
    public GameObject starsContainer;
    public Image[] starImages;
    public Sprite starAchievedSprite;
    public Sprite starUnachievedSprite;

    [Header("Buttons")]
    public Button nextLevelButton;
    public Button retryButton;
    public Button mainMenuButton;

    private string nextLevelToLoadIfWon;
    private string currentLevelPlayedFileName;
    private bool canPlayNextLevelAfterWin = false;

    void Awake()
    {
        if (nextLevelButton)
            nextLevelButton.onClick.AddListener(OnNextLevelPressed);

        if (retryButton)
            retryButton.onClick.AddListener(OnRetryPressed);

        if (mainMenuButton)
            mainMenuButton.onClick.AddListener(OnMainMenuPressed);

        if (starsContainer != null)
        {
            starsContainer.SetActive(true);

            if (starImages != null)
            {
                foreach (Image starImg in starImages)
                {
                    if (starImg != null)
                        starImg.gameObject.SetActive(true);
                }
            }
        }
    }

    public void SetupResult(bool levelWon, int starsAchieved, float timeLeft, string currentLevelFileName, string nextLevelFileNameIfWon)
    {
        currentLevelPlayedFileName = currentLevelFileName;
        nextLevelToLoadIfWon = nextLevelFileNameIfWon;

        if (winImage != null)
            winImage.gameObject.SetActive(levelWon);

        if (loseImage != null)
            loseImage.gameObject.SetActive(!levelWon);

        if (starsContainer != null && starImages != null && starImages.Length == 3)
        {
            starsContainer.SetActive(true);
            for (int i = 0; i < 3; i++)
            {
                starImages[i].gameObject.SetActive(true);

                if (levelWon)
                    starImages[i].sprite = (i < starsAchieved) ? starAchievedSprite : starUnachievedSprite;
                else
                    starImages[i].sprite = starUnachievedSprite;
            }
        }

        if (levelWon)
        {
            canPlayNextLevelAfterWin = false;

            if (nextLevelButton != null)
            {
                if (!string.IsNullOrEmpty(nextLevelFileNameIfWon) && GameManager.Instance != null)
                {
                    string resourcePathForNextLevel = Path.Combine(GameManager.Instance.levelsResourceSubFolder, nextLevelFileNameIfWon);
                    TextAsset nextLevelAsset = Resources.Load<TextAsset>(resourcePathForNextLevel);
                    canPlayNextLevelAfterWin = (nextLevelAsset != null);
                }

                nextLevelButton.gameObject.SetActive(canPlayNextLevelAfterWin);
            }
        }
        else
        {
            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(false);

            canPlayNextLevelAfterWin = false;
        }

        if (retryButton)
            retryButton.gameObject.SetActive(true);

        if (mainMenuButton)
            mainMenuButton.gameObject.SetActive(true);
    }

    void OnNextLevelPressed()
    {
        if (canPlayNextLevelAfterWin && !string.IsNullOrEmpty(nextLevelToLoadIfWon))
        {
            if (UIManager.Instance != null && GameManager.Instance != null)
            {
                UIManager.Instance.ShowGameplay();
                GameManager.Instance.StartOrRestartLevel(nextLevelToLoadIfWon);
            }
        }
    }

    void OnRetryPressed()
    {
        if (UIManager.Instance != null && GameManager.Instance != null)
        {
            UIManager.Instance.ShowGameplay();
            GameManager.Instance.StartOrRestartLevel(currentLevelPlayedFileName);
        }
    }

    void OnMainMenuPressed()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowMainMenu();
    }
}
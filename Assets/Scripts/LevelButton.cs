using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public TextMeshProUGUI levelNameText;
    public GameObject starsContainer;
    public Image[] starImages;
    public GameObject lockOverlay;
    public Button buttonComponent;

    public Sprite starAchievedSprite;
    public Sprite starUnachievedSprite;

    private string levelFileNameToLoad;

    public void Setup(string displayName, string levelFileName, int starRating, bool isLocked)
    {
        this.levelFileNameToLoad = levelFileName;

        if (levelNameText != null)
        {
            levelNameText.text = displayName;
            levelNameText.gameObject.SetActive(true);
        }

        if (lockOverlay != null)
            lockOverlay.SetActive(isLocked);

        buttonComponent.interactable = !isLocked;

        Mask mask = GetComponent<Mask>() ?? GetComponentInChildren<Mask>();

        if (mask != null)
            mask.enabled = isLocked;

        if (starsContainer != null)
        {
            if (isLocked)
                starsContainer.SetActive(false);
            else
            {
                starsContainer.SetActive(true);

                if (starRating > 0)
                {
                    for (int i = 0; i < starImages.Length; i++)
                    {
                        starImages[i].gameObject.SetActive(true);
                        if (i < starRating)
                            starImages[i].sprite = starAchievedSprite;
                        else
                            starImages[i].sprite = starUnachievedSprite;
                    }
                }
                else
                    starsContainer.SetActive(false);
            }
        }

        buttonComponent.onClick.RemoveAllListeners();

        if (!isLocked)
            buttonComponent.onClick.AddListener(LoadLevel);
    }

    void LoadLevel()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameplay();

        if (GameManager.Instance != null)
            GameManager.Instance.StartOrRestartLevel(levelFileNameToLoad);
    }
}
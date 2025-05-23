using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class LevelSelectionManager : MonoBehaviour
{
    public GameObject levelButtonPrefab;
    public Transform buttonParent;
    public string levelsResourcePath = "Levels";

    public Sprite starAchievedSpriteRef;
    public Sprite starUnachievedSpriteRef;

    public static List<string> SortedLevelFileNames { get; private set; } = new List<string>();
    private static bool staticListInitialized = false;

    void Awake()
    {
        EnsureLevelOrderInitialized();
    }

    public void PopulateLevels()
    {
        EnsureLevelOrderInitialized();

        for (int i = buttonParent.childCount - 1; i >= 0; i--)
            Destroy(buttonParent.GetChild(i).gameObject);

        if (SortedLevelFileNames.Count == 0)
            return;

        for (int i = 0; i < SortedLevelFileNames.Count; i++)
        {
            string levelFileNameKey = SortedLevelFileNames[i];
            string displayName = $"Level {i + 1}";

            GameObject buttonGO = Instantiate(levelButtonPrefab, buttonParent);
            LevelButton levelButtonScript = buttonGO.GetComponent<LevelButton>();

            if (levelButtonScript != null)
            {
                levelButtonScript.starAchievedSprite = starAchievedSpriteRef;
                levelButtonScript.starUnachievedSprite = starUnachievedSpriteRef;

                int stars = LevelProgressionManager.GetStarRating(levelFileNameKey);
                bool isLocked;

                if (i == 0)
                {
                    isLocked = false;

                    if (!LevelProgressionManager.IsLevelUnlocked(levelFileNameKey))
                        LevelProgressionManager.UnlockLevel(levelFileNameKey);
                }
                else
                {
                    string previousLevelInSortedOrder_FileNameKey = SortedLevelFileNames[i - 1];
                    bool previousLevelWon = LevelProgressionManager.GetStarRating(previousLevelInSortedOrder_FileNameKey) > 0;

                    if (previousLevelWon && !LevelProgressionManager.IsLevelUnlocked(levelFileNameKey))
                        LevelProgressionManager.UnlockLevel(levelFileNameKey);

                    isLocked = !LevelProgressionManager.IsLevelUnlocked(levelFileNameKey);
                }

                levelButtonScript.Setup(displayName, levelFileNameKey, stars, isLocked);
            }
        }
    }

    public static void EnsureLevelOrderInitialized()
    {
        if (staticListInitialized)
            return;

        string path = "Levels";
        LevelSelectionManager instance = FindObjectOfType<LevelSelectionManager>();
        if (instance != null) path = instance.levelsResourcePath;


        SortedLevelFileNames.Clear();
        TextAsset[] levelAssets = Resources.LoadAll<TextAsset>(path);

        if (levelAssets == null || levelAssets.Length == 0)
        {
            staticListInitialized = true;
            return;
        }

        List<KeyValuePair<int, string>> levelsToSort = new List<KeyValuePair<int, string>>();
        foreach (TextAsset asset in levelAssets)
        {
            if (asset != null)
            {
                int levelNumber = GetLevelNumberFromName(asset.name);
                levelsToSort.Add(new KeyValuePair<int, string>(levelNumber, asset.name));
            }
        }

        levelsToSort.Sort((pair1, pair2) => pair1.Key.CompareTo(pair2.Key));

        foreach (var kvp in levelsToSort)
            SortedLevelFileNames.Add(kvp.Value);

        staticListInitialized = true;
    }

    public static int GetLevelNumberFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return int.MaxValue;

        Match match = Regex.Match(name, @"(\d+)");

        if (match.Success)
        {
            if (int.TryParse(match.Value, out int num))
                return num;
        }

        return int.MaxValue;
    }

    public static string GetNextLevelInSequence(string currentLevelFileName)
    {
        EnsureLevelOrderInitialized();

        if (SortedLevelFileNames.Count == 0)
            return null;

        int currentIndex = SortedLevelFileNames.IndexOf(currentLevelFileName);

        if (currentIndex != -1 && currentIndex + 1 < SortedLevelFileNames.Count)
            return SortedLevelFileNames[currentIndex + 1];

        return null;
    }

    public static int GetDisplayIndexOfLevel(string levelFileName)
    {
        EnsureLevelOrderInitialized();

        if (SortedLevelFileNames.Count == 0)
            return -1;

        return SortedLevelFileNames.IndexOf(levelFileName);
    }
}
using UnityEngine;

public static class LevelProgressionManager
{
    private const string RATING_PREFIX = "LevelRating_";
    private const string UNLOCKED_PREFIX = "LevelUnlocked_";

    public static void SetStarRating(string levelName, int stars)
    {
        if (stars < 0 || stars > 3)
            return;

        if (stars > GetStarRating(levelName))
        {
            PlayerPrefs.SetInt(RATING_PREFIX + levelName, stars);
            PlayerPrefs.Save();
        }
    }

    public static int GetStarRating(string levelName)
    {
        return PlayerPrefs.GetInt(RATING_PREFIX + levelName, 0);
    }

    public static void UnlockLevel(string levelName)
    {
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + levelName, 1);
        PlayerPrefs.Save();
    }

    public static bool IsLevelUnlocked(string levelName)
    {
        if (levelName.EndsWith("01"))
            return true;

        return PlayerPrefs.GetInt(UNLOCKED_PREFIX + levelName, 0) == 1;
    }

    public static void UnlockNextLevel(string currentLevelName)
    {
        string[] parts = currentLevelName.Split('_');

        if (parts.Length == 2 && int.TryParse(parts[1], out int currentLevelNum))
        {
            int nextLevelNum = currentLevelNum + 1;
            string nextLevelName = $"{parts[0]}_{nextLevelNum:D2}";
            UnlockLevel(nextLevelName);
        }
    }

    public static void ResetAllProgression()
    {
        Debug.LogWarning("Resetting ALL Player Progression!");
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
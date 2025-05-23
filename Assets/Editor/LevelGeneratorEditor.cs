#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorEditor : Editor
{
    private int currentSeed;
    private string suggestedBaseLevelName = "Level_01";
    private string savePath = "Assets/Resources/Levels/";

    private int targetMinSolutionPath = 3;
    private int targetMaxSolutionPath = 6;
    private bool usePathLengthTargeting = false;

    private void OnEnable()
    {
        LevelGenerator generator = (LevelGenerator)target;

        if (generator != null)
        {
            if (currentSeed == 0)
                currentSeed = generator.RequestNewUnusedSeed();

            SuggestNextBaseLevelName();
        }
    }

    private void SuggestNextBaseLevelName()
    {
        if (!string.IsNullOrEmpty(savePath) && !savePath.EndsWith("/") && !savePath.EndsWith("\\"))
            savePath += Path.DirectorySeparatorChar;

        if (!Directory.Exists(savePath))
        {
            suggestedBaseLevelName = "Level_01";
            return;
        }

        string[] files = Directory.GetFiles(savePath, "*.json");

        if (files.Length == 0)
        {
            suggestedBaseLevelName = "Level_01";
            return;
        }

        int highestLevelNum = 0;

        foreach (string file in files)
        {
            string fn = Path.GetFileNameWithoutExtension(file);
            Match match = Regex.Match(fn, @"(?:Level_|Lvl_|Level|L)?(\d+)");
            if (match.Success && match.Groups.Count > 1 && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                if (int.TryParse(match.Groups[1].Value, out int num))
                {
                    if (num > highestLevelNum)
                        highestLevelNum = num;
                }
            }
        }

        suggestedBaseLevelName = $"Level_{highestLevelNum + 1:D2}";
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        LevelGenerator generator = (LevelGenerator)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Generation & Saving", EditorStyles.boldLabel);

        currentSeed = EditorGUILayout.IntField("Current Seed", currentSeed);
        if (GUILayout.Button("Get New Random Seed"))
        {
            currentSeed = generator.RequestNewUnusedSeed();
            GUI.FocusControl(null);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Optional Difficulty Filtering (Solution Length)", EditorStyles.boldLabel);
        usePathLengthTargeting = EditorGUILayout.Toggle("Filter by Path Length", usePathLengthTargeting);
        EditorGUI.BeginDisabledGroup(!usePathLengthTargeting);
        targetMinSolutionPath = EditorGUILayout.IntField("Min Solution Path", targetMinSolutionPath);
        targetMaxSolutionPath = EditorGUILayout.IntField("Max Solution Path", targetMaxSolutionPath);
        EditorGUI.EndDisabledGroup();

        if (targetMinSolutionPath < 1 && usePathLengthTargeting)
            targetMinSolutionPath = 1;

        if (targetMaxSolutionPath < targetMinSolutionPath && usePathLengthTargeting)
            targetMaxSolutionPath = targetMinSolutionPath;

        EditorGUILayout.Space();
        suggestedBaseLevelName = EditorGUILayout.TextField("Level Filename (e.g., Level_01)", suggestedBaseLevelName);

        if (GUILayout.Button("Suggest Next Level Filename"))
        {
            SuggestNextBaseLevelName();
            GUI.FocusControl(null);
        }

        savePath = EditorGUILayout.TextField("Save Directory Path", savePath);

        if (!string.IsNullOrEmpty(savePath) && !savePath.EndsWith("/") && !savePath.EndsWith("\\"))
            savePath += Path.DirectorySeparatorChar;

        if (GUILayout.Button("Generate and Save Level"))
        {
            if (string.IsNullOrEmpty(suggestedBaseLevelName))
            {
                EditorUtility.DisplayDialog("Error", "Level Filename cannot be empty.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                EditorUtility.DisplayDialog("Error", "Save Directory Path cannot be empty.", "OK");
                return;
            }

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            LevelData data;

            if (usePathLengthTargeting)
                data = generator.GenerateLevel(currentSeed, suggestedBaseLevelName, targetMinSolutionPath, targetMaxSolutionPath);
            else
                data = generator.GenerateLevel(currentSeed, suggestedBaseLevelName);

            if (data != null)
            {
                string fullPath = Path.Combine(savePath, data.levelName + ".json");

                if (File.Exists(fullPath))
                {
                    if (!EditorUtility.DisplayDialog("Overwrite Level?", $"File {data.levelName}.json already exists. Overwrite?", "Yes", "No"))
                        return;
                }

                generator.SaveLevelToJson(data, fullPath);
                AssetDatabase.Refresh();
                currentSeed = generator.RequestNewUnusedSeed();
                SuggestNextBaseLevelName();
                EditorUtility.SetDirty(target);
                GUI.FocusControl(null);
            }
            else
                EditorUtility.DisplayDialog("Generation Failed", $"Could not generate a suitable level with seed {currentSeed} (and target path lengths if specified).", "OK");
        }
    }
}
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.CloudSave;
using Unity.Services.Leaderboards;
using TMPro;
using System;
using System.Threading.Tasks;

public class LevelSelect : MonoBehaviour
{
    public static LevelSelect instance;
    public GameObject levelSelectMenu;
    public List<Level> levelsToReload = new List<Level>();
    public TextMeshProUGUI titleOrLoadingText;
    [SerializeField]
    private GameObject leaderboardEnableButton;
    public LeaderboardTierColorset unbeatenColorset;
    public LeaderboardTierColorset whiteTierColorset;
    public LeaderboardTierColorset bronzeTierColorset;
    public LeaderboardTierColorset silverTierColorset;
    public LeaderboardTierColorset goldTierColorset;
    public LeaderboardTierColorset purpleTierColorset;
    

    public Level[,] levels = {
        {new Level(1, 1),
        new Level(1, 2),
        new Level(1, 3),
        new Level(1, 4),
        new Level(1, 5)},
        {new Level(2, 1),
        new Level(2, 2),
        new Level(2, 3),
        new Level(2, 4),
        new Level(2, 5)},
        {new Level(3, 1),
        new Level(3, 2),
        new Level(3, 3),
        new Level(3, 4),
        new Level(3, 5)},
        {new Level(4, 1),
        new Level(4, 2),
        new Level(4, 3),
        new Level(4, 4),
        new Level(4, 5)}
    };

    public bool loading = false;

    public Dictionary<Level, Button> levelButtons = new Dictionary<Level, Button>();

    private void Start() {
        if (instance == null)
        {
            loading = true;
            instance = this;
        }
        else
            Destroy(gameObject);
        // titleOrLoadingText.text = "Level Select";

        StartCoroutine(LoadLevelsCoroutine());
    }

    public void ShowButtons()
    {
        foreach (Button button in levelSelectMenu.GetComponentsInChildren<Button>())
        {
            button.enabled = true;
            button.GetComponent<Image>().enabled = true;
            button.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
        }
    }

    public void HideButtons()
    {
        foreach (Button button in levelSelectMenu.GetComponentsInChildren<Button>())
        {
            button.enabled = false;
            button.GetComponent<Image>().enabled = false;
            button.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
        }
    }

    private IEnumerator LoadLevelsCoroutine()
    {
        while (Settings.instance == null || !Settings.instance.loaded)
        {
            yield return new WaitForSeconds(0.01f);
        }
        LoadLevels();
        // titleOrLoadingText.text = "Level Select";
    }

    private async void LoadLevels()
    {
        loading = true;

        foreach (Button levelButton in levelSelectMenu.GetComponentsInChildren<Button>())
        {
            float trophyVertOffset = 1;
            float trophyHorzOffset = 1;
            if (levelButton.gameObject == null || !levelButton.name.Contains("Level"))
                continue;

            int world = int.Parse(levelButton.transform.parent.name.Substring(6, 1));
            int levelNum = int.Parse(levelButton.name.Substring(6, 1));

            Level level = levels[world - 1, levelNum - 1];
            levelButtons.Add(level, levelButton);

            Button trophyButton = Instantiate(leaderboardEnableButton,
                levelButton.transform.position + new Vector3(67 * trophyHorzOffset, 67 * trophyVertOffset, 0),
                Quaternion.identity, levelButton.transform).GetComponent<Button>();

            trophyButton.transform.localScale = Vector3.one;
            trophyButton.onClick.AddListener(() => Leaderboard.instance.ShowLeaderboard(level));
            levelButton.onClick.AddListener(() => LoadLevel(level.ToString()));
        }

        // Load all level button data in parallel
        List<Task> loadTasks = new List<Task>();
        foreach (Level level in levels)
        {
            loadTasks.Add(LoadLevelButton(level));
        }

        await Task.WhenAll(loadTasks); // Wait for all level data to load
        loading = false;
    }


    private async Task LoadLevelButton(Level level)
    {
        string levelTitle = "W" + level.world + "L" + level.level;
        var playerData = await CloudSaveService.Instance.Data.Player
            .LoadAsync(new HashSet<string> { levelTitle });

        if (!levelButtons.TryGetValue(level, out Button levelButton))
            return;

        if (playerData.TryGetValue(levelTitle, out var levelTime))
        {
            level.bestTime = levelTime.Value.GetAs<float>();
            level.beaten = true;
            levelButton.GetComponentInChildren<TextMeshProUGUI>().text
                = "Level " + level.level + Environment.NewLine
                + Environment.NewLine + level.bestTime.ToString("F2") + "s";
        }

        SetButtonColors(level, levelButton);
    }



    public void ReloadLevelTime(Level level)
    {
        loading = true;
        // titleOrLoadingText.text = "Loading Level Times...";
        float time = level.bestTime;
        level = levels[level.world - 1, level.level - 1];
        Debug.Log("Adjusting level time for " + level.world + " " + level.level);
        if (levelButtons.ContainsKey(level))
        {
            Button levelButton = levelButtons[level];
            levelButton.GetComponentInChildren<TextMeshProUGUI>().text = "Level " + level.level + Environment.NewLine
            + Environment.NewLine + time.ToString("F2") + "s";
            SetButtonColors(level, levelButton);
        }
        loading = false;
    }

    private async void SetButtonColors(Level level, Button levelButton)
    {
        ColorBlock buttonColorBlock = new ColorBlock();
        ColorBlock trophyColorBlock = new ColorBlock();
        buttonColorBlock.colorMultiplier = 1;
        trophyColorBlock.colorMultiplier = 1;
        Color textColor = Color.black;
        if (!level.beaten)
        {
            buttonColorBlock.normalColor = unbeatenColorset.normalColor;
            buttonColorBlock.highlightedColor = unbeatenColorset.highlightedColor;
            buttonColorBlock.pressedColor = unbeatenColorset.pressedColor;
            buttonColorBlock.selectedColor = unbeatenColorset.selectedColor;
            trophyColorBlock.normalColor = unbeatenColorset.trophyNormalColor;
            trophyColorBlock.highlightedColor = unbeatenColorset.trophyHighlightedColor;
            trophyColorBlock.pressedColor = unbeatenColorset.trophyPressedColor;
            trophyColorBlock.selectedColor = unbeatenColorset.trophySelectedColor;
            textColor = unbeatenColorset.textColor;
            // Debug.Log("Setting " + level.ToString() + " to unbeaten colors");
        }
        else
        {
            LeaderboardEntry worldRecord = await Leaderboard.instance.GetWorldRecord(level);
            if (!Settings.instance.participateInLeaderboard || level.bestTime - 10 > worldRecord.Time)
            {
                buttonColorBlock.normalColor = whiteTierColorset.normalColor;
                buttonColorBlock.highlightedColor = whiteTierColorset.highlightedColor;
                buttonColorBlock.pressedColor = whiteTierColorset.pressedColor;
                buttonColorBlock.selectedColor = whiteTierColorset.selectedColor;
                trophyColorBlock.normalColor = whiteTierColorset.trophyNormalColor;
                trophyColorBlock.highlightedColor = whiteTierColorset.trophyHighlightedColor;
                trophyColorBlock.pressedColor = whiteTierColorset.trophyPressedColor;
                trophyColorBlock.selectedColor = whiteTierColorset.trophySelectedColor;
                textColor = whiteTierColorset.textColor;
                // Debug.Log("Setting " + level.ToString() + " to white colors");
            }
            else if (level.bestTime - 3 > worldRecord.Time)
            {
                buttonColorBlock.normalColor = bronzeTierColorset.normalColor;
                buttonColorBlock.highlightedColor = bronzeTierColorset.highlightedColor;
                buttonColorBlock.pressedColor = bronzeTierColorset.pressedColor;
                buttonColorBlock.selectedColor = bronzeTierColorset.selectedColor;
                trophyColorBlock.normalColor = bronzeTierColorset.trophyNormalColor;
                trophyColorBlock.highlightedColor = bronzeTierColorset.trophyHighlightedColor;
                trophyColorBlock.pressedColor = bronzeTierColorset.trophyPressedColor;
                trophyColorBlock.selectedColor = bronzeTierColorset.trophySelectedColor;
                textColor = bronzeTierColorset.textColor;
                // Debug.Log("Setting " + level.ToString() + " to bronze colors");
            }
            else if (level.bestTime - 1 > worldRecord.Time)
            {
                buttonColorBlock.normalColor = silverTierColorset.normalColor;
                buttonColorBlock.highlightedColor = silverTierColorset.highlightedColor;
                buttonColorBlock.pressedColor = silverTierColorset.pressedColor;
                buttonColorBlock.selectedColor = silverTierColorset.selectedColor;
                trophyColorBlock.normalColor = silverTierColorset.trophyNormalColor;
                trophyColorBlock.highlightedColor = silverTierColorset.trophyHighlightedColor;
                trophyColorBlock.pressedColor = silverTierColorset.trophyPressedColor;
                trophyColorBlock.selectedColor = silverTierColorset.trophySelectedColor;
                textColor = silverTierColorset.textColor;
                // Debug.Log("Setting " + level.ToString() + " to silver colors");
            }
            else if (level.bestTime > worldRecord.Time)
            {
                buttonColorBlock.normalColor = goldTierColorset.normalColor;
                buttonColorBlock.highlightedColor = goldTierColorset.highlightedColor;
                buttonColorBlock.pressedColor = goldTierColorset.pressedColor;
                buttonColorBlock.selectedColor = goldTierColorset.selectedColor;
                trophyColorBlock.normalColor = goldTierColorset.trophyNormalColor;
                trophyColorBlock.highlightedColor = goldTierColorset.trophyHighlightedColor;
                trophyColorBlock.pressedColor = goldTierColorset.trophyPressedColor;
                trophyColorBlock.selectedColor = goldTierColorset.trophySelectedColor;
                textColor = goldTierColorset.textColor;
                // Debug.Log("Setting " + level.ToString() + " to gold colors");
            }
            else // if (level.bestTime - 0 <= worldRecord.Time)
            {
                buttonColorBlock.normalColor = purpleTierColorset.normalColor;
                buttonColorBlock.highlightedColor = purpleTierColorset.highlightedColor;
                buttonColorBlock.pressedColor = purpleTierColorset.pressedColor;
                buttonColorBlock.selectedColor = purpleTierColorset.selectedColor;
                trophyColorBlock.normalColor = purpleTierColorset.trophyNormalColor;
                trophyColorBlock.highlightedColor = purpleTierColorset.trophyHighlightedColor;
                trophyColorBlock.pressedColor = purpleTierColorset.trophyPressedColor;
                trophyColorBlock.selectedColor = purpleTierColorset.trophySelectedColor;
                textColor = purpleTierColorset.textColor;
                // Debug.Log("Setting " + level.ToString() + " to purple colors");
            }
        }
        levelButton.colors = buttonColorBlock;
        levelButton.GetComponentInChildren<TextMeshProUGUI>().color = textColor;
        Button trophyButton = levelButton.GetComponentsInChildren<Button>()[1];
        trophyButton.colors = trophyColorBlock;

    }

    /// <summary>
    /// Gets the first level that has not been beaten yet or returns null if all levels have been beaten.
    /// </summary>
    /// <returns>Earliest unbeaten level or null if all beaten</returns>
    public Level GetNextUnbeatenLevel()
    {
        foreach (Level level in levels)
        {
            if (!level.beaten && level.world < 4)
            {
                return level;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the next level in the current world or the first level of the next world.
    /// If the current level is the last level of the last world, returns null.
    /// </summary>
    /// <param name="currentLevel">What level you want the level after</param>
    /// <returns>The level after the given level or null if there is no next level</returns>
    /// <summary>
    /// Gets either the next level in current world or first level of the next world
    /// </summary>
    /// <returns>Next level</returns>
    public Level GetNextLevel(Level currentLevel)
    {
        int currWorld = currentLevel.world;
        int currLevel = currentLevel.level;
        if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("W" + currWorld + "L" + (currLevel + 1)) != -1)
        {
            return levels[currWorld - 1, currLevel]; //"W" + currWorld + "L" + (currLevel + 1);
        }
        else if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("W" + (currWorld + 1) + "L1") != -1)
        {
            return levels[currWorld, 0]; // "W" + (currWorld + 1) + "L1";
        }
        else
        {
            // Debug.LogError("No next level found for " + "W" + currWorld + "L" + (currLevel + 1)
            //  + " or W" + (currWorld + 1) + "L1");
            
            return null;
        }
    }

    public void LoadLevel(Level level)
    {
        if (loading)
            return;
        UnityEngine.SceneManagement.SceneManager.LoadScene(level.ToString());
        if (Settings.instance.showTimer) Timer.instance.timerText.enabled = true;
        else Timer.instance.timerText.enabled = false;
        gameObject.SetActive(false);
        // StartCoroutine(WaitForRemovePlayer());
    }

    public void LoadLevel(string level)
    {
        if (loading)
            return;
        UnityEngine.SceneManagement.SceneManager.LoadScene(level);
        gameObject.SetActive(false);
        if (Settings.instance.showTimer) Timer.instance.timerText.enabled = true;
        else Timer.instance.timerText.enabled = false;
        // StartCoroutine(WaitForRemovePlayer());
    }

    public Level GetLevelByName(string name)
    {
        int lIndex = name.IndexOf('L');
        Debug.Assert(lIndex >= 0, "Couldn't find L in level name");
        int worldNum = int.Parse(name.Substring(1, lIndex - 1));
        int levelNum = int.Parse(name.Substring(lIndex + 1, name.Length - 1 - lIndex));
        return levels[worldNum - 1, levelNum - 1];
    }
}
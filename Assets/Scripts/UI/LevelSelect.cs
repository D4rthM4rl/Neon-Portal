using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

public class LevelSelect : MonoBehaviour
{
    /// <summary>Singleton instance of the LevelSelect.</summary>
    public static LevelSelect instance;
    /// <summary>The level select menu GameObject which contains all the level buttons.</summary>
    public GameObject levelSelectMenu;
    /// <summary>Which levels to reload the times for when returning to level select menu.</summary>
    public List<Level> levelsToReload = new List<Level>();
    /// <summary>Text element to show "Level Select" or loading.</summary>
    public TextMeshProUGUI titleOrLoadingText;
    /// <summary>The button gameObject to show the leaderboard</summary>
    [SerializeField] private GameObject leaderboardEnableButton;
    /// <summary>Colorset for the unbeaten tier.</summary>
    public LeaderboardTierColorset unbeatenColorset;
    public LeaderboardTierColorset whiteTierColorset;
    public LeaderboardTierColorset bronzeTierColorset;
    public LeaderboardTierColorset silverTierColorset;
    public LeaderboardTierColorset goldTierColorset;
    public LeaderboardTierColorset purpleTierColorset;
    
    /// <summary>All levels in the game, indexed by [world-1, level-1].</summary>
    public Level[,] levels = {
        {new Level(1, 1),
        new Level(1, 2),
        new Level(1, 3),
        new Level(1, 4),
        new Level(1, 5),
        new Level(1, 6)},
        {new Level(2, 1),
        new Level(2, 2),
        new Level(2, 3),
        new Level(2, 4),
        new Level(2, 5),
        new Level(2, 6)},
        {new Level(3, 1),
        new Level(3, 2),
        new Level(3, 3),
        new Level(3, 4),
        new Level(3, 5),
        new Level(3, 6)},
        {new Level(4, 1),
        new Level(4, 2),
        new Level(4, 3),
        new Level(4, 4),
        new Level(4, 5),
        new Level(4, 6)}
    };
    /// <summary>Whether the level select is still loading data.</summary>
    public bool loading = false;
    /// <summary>Mapping of levels to their corresponding buttons for easy access.</summary>
    public Dictionary<Level, Button> levelButtons = new Dictionary<Level, Button>();

    private void Awake()
    {
        if (instance == null)
        {
            loading = true;
            instance = this;
        }
        else
        {
            Debug.Log("LevelSelect instance already exists, destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start() {
        
        // titleOrLoadingText.text = "Level Select";

        StartCoroutine(LoadLevelsCoroutine());
    }

    /// <summary>Enables the buttons in the level select menu.</summary>
    public void ShowButtons()
    {
        foreach (Button button in levelSelectMenu.GetComponentsInChildren<Button>())
        {
            button.enabled = true;
            button.GetComponent<Image>().enabled = true;
            button.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
        }
    }

    /// <summary>Disables/hides the buttons in the level select menu.</summary>
    public void HideButtons()
    {
        foreach (Button button in levelSelectMenu.GetComponentsInChildren<Button>())
        {
            button.enabled = false;
            button.GetComponent<Image>().enabled = false;
            button.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
        }
    }

    /// <summary>Coroutine to load levels after settings have been loaded.</summary>
    private IEnumerator LoadLevelsCoroutine()
    {
        while (Settings.instance == null || !Settings.instance.loaded)
        {
            yield return new WaitForSeconds(0.01f);
        }
        LoadLevels();
        // titleOrLoadingText.text = "Level Select";
    }

    /// <summary>Loads all levels' data and updates their buttons accordingly.</summary>
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
            trophyButton.onClick.AddListener(() => Leaderboard.instance.ShowLevelSelectLeaderboard(level));
            levelButton.onClick.AddListener(() => StartCoroutine(LoadLevel(level.ToString())));
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

    public async void ReloadAllLevels()
    {
        // Load all level button data in parallel
        List<Task> loadTasks = new List<Task>();
        foreach (Level level in levels)
        {
            loadTasks.Add(LoadLevelButton(level));
        }

        await Task.WhenAll(loadTasks); // Wait for all level data to load
    }

    /// <summary>Sets up a button (color and time) for a level.</summary>
    /// <param name="level">Level of the button to set up.</param>
    private Task LoadLevelButton(Level level)
    {
        string levelTitle = "W" + level.world + "L" + level.level;

        if (!levelButtons.TryGetValue(level, out Button levelButton))
        {
            Debug.LogError($"Level button for {levelTitle} not found.");
            return Task.CompletedTask;
        }

        if (PlayerPrefs.HasKey(levelTitle))
        {
            level.bestTime = PlayerPrefs.GetFloat(levelTitle);
            level.beaten = true;

            levelButton.GetComponentInChildren<TextMeshProUGUI>().text
                = "Level " + level.level + Environment.NewLine
                + Environment.NewLine + level.bestTime.ToString("F2") + "s";
        }

        SetButtonColors(level, levelButton);
        return Task.CompletedTask;
    }

    /// <summary>Reload a time on the menu for a level because the best time has changed.</summary>
    /// <param name="level">Level to have the time reloaded for.</param>
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

    /// <summary>
    /// Sets the colors of the button for each level in the menu based on the
    /// user's completion times compared to the best.
    /// </summary>
    /// <param name="level">Level to look at.</param>
    /// <param name="levelButton">The button to change the color of.</param>
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
            if (!OnlineServices.online)
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
                return;
            }
            else
            {
                while (Leaderboard.instance == null)
                {
                    await Task.Delay(1); // Wait for Leaderboard to initialize
                }
                LeaderboardEntry worldRecord = await Leaderboard.instance.GetWorldRecord(level);
                if (!Settings.instance.participateInLeaderboard 
                    || level.bestTime - 10 > worldRecord.Time
                    || !OnlineServices.online)
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
        }
        levelButton.colors = buttonColorBlock;
        levelButton.GetComponentInChildren<TextMeshProUGUI>().color = textColor;
        Button trophyButton = levelButton.GetComponentsInChildren<Button>()[1];
        trophyButton.colors = trophyColorBlock;

    }

    /// <summary>
    /// Gets the first level that has not been beaten yet or returns null if all levels have been beaten.
    /// </summary>
    /// <returns>Earliest unbeaten level or null if all beaten.</returns>
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
    /// <param name="currentLevel">What level you want the level after.</param>
    /// <returns>The level after the given level or null if there is no next level.</returns>
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

    /// <summary>Loads a level to be played.</summary>
    /// <param name="level">Level to be played.</param>
    public IEnumerator LoadLevel(Level level)
    {
        if (loading)
            yield return null;

        float fadeDuration = Transition.instance.fadeDuration;
        yield return StartCoroutine(Transition.instance.FadeAsync(0f, 1f, fadeDuration/2)); // Fade out
        Transition.instance.LoadLevelFromLevelSelect(level);
        if (Settings.instance.showTimer) Timer.instance.timerText.enabled = true;
        else Timer.instance.timerText.enabled = false;
        gameObject.SetActive(false);
    }

    /// <summary>Loads a level to be played.</summary>
    /// <param name="level">Name of the level to be played.</param>
    public IEnumerator LoadLevel(string level)
    {
        yield return StartCoroutine(LoadLevel(GetLevelByName(level)));
    }

    /// <summary>Gets a level by its name in L1W1 form.</summary>
    /// <param name="name">Name of the level.</param>
    /// <returns>Level which was named.</returns>
    public Level GetLevelByName(string name)
    {
        int lIndex = name.IndexOf('L');
        Debug.Assert(lIndex >= 0, "Couldn't find L in level name");
        int worldNum = int.Parse(name.Substring(1, lIndex - 1));
        int levelNum = int.Parse(name.Substring(lIndex + 1, name.Length - 1 - lIndex));
        return levels[worldNum - 1, levelNum - 1];
    }
}
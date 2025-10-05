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
    /// <summary>The parent GameObject that contains all the level buttons.</summary>
    [SerializeField] private GameObject levelButtonsParent;
    /// <summary>Prefab for 1 star decoration on a level button</summary>
    [SerializeField] private GameObject star1Decoration;
    /// <summary>Prefab for 2 star decoration on a level button</summary>
    [SerializeField] private GameObject star2Decoration;
    /// <summary>Prefab for 3 star decoration on a level button</summary>
    [SerializeField] private GameObject star3Decoration;
    /// <summary>The menu that appears when a level is selected, showing the leaderboard and play button.</summary>
    [SerializeField] private GameObject levelSelectedMenu;
    /// <summary>Which levels to reload the times for when returning to level select menu.</summary>
    public List<Level> levelsToReload = new List<Level>();
    /// <summary>Text element to show "Level Select" or current Level selected.</summary>
    public TextMeshProUGUI titleText;
    /// <summary>Button to start playing selected level.</summary>
    [SerializeField] private Button playButton;
    [SerializeField] private Button myRanksButton;
    [SerializeField] private Button starsButton;
    [SerializeField] private Button top20Button;

    [SerializeField] private TextMeshProUGUI totalDeaths;
    [SerializeField] private TextMeshProUGUI totalResets;
    [SerializeField] private TextMeshProUGUI totalTime;
    /// <summary>Color for the outline of an unbeaten level.</summary>
    public Color unbeatenColor;
    public Color grayTierColor;
    public Color purpleTierColor;
    public Color blueTierColor;
    public Color greenTierColor;
    
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
    public Dictionary<Level, LevelButton> levelButtons = new Dictionary<Level, LevelButton>();

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

    private void Start() 
    {
        StartCoroutine(LoadLevelsCoroutine());
    }

    /// <summary>Displays the level screen with leaderboard and play button for a selected level</summary>
    private void SelectLevel(Level level)
    {
        titleText.text = "World " + level.world + " Level " + level.level;
        totalDeaths.text = "Deaths: " + level.totalDeaths;
        totalResets.text = "Resets: " + level.totalResets;
        float time = level.totalTime;
        if (time % 60 < 10) // For stuff like 1:05 instead of 1:5
            totalTime.text = $"Time Played: \n" + (int)time / 60 + ":0" + (time % 60).ToString("F2");
        else 
            totalTime.text = $"Time Played: \n" + (int)time / 60 + ":" + (time % 60).ToString("F2");
        levelSelectedMenu.SetActive(true);
        levelButtonsParent.SetActive(false);
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(() => StartCoroutine(LoadLevel(level)));
        myRanksButton.onClick.RemoveAllListeners();
        myRanksButton.onClick.AddListener(() => Leaderboard.instance.ShowLevelSelectMyRanks(level));
        starsButton.onClick.RemoveAllListeners();
        starsButton.onClick.AddListener(() => Leaderboard.instance.ShowLevelSelectStars(level, level.bestTime));
        top20Button.onClick.RemoveAllListeners();
        top20Button.onClick.AddListener(() => Leaderboard.instance.ShowLevelSelectTop20(level));

        Leaderboard.instance.ShowLevelSelectStars(level, level.bestTime);
    }

    /// <summary>Takes us off the level selected menu for the current selected level.</summary>
    public void UnselectLevel()
    {
        titleText.text = "Level SelecT";
        levelSelectedMenu.SetActive(false);
        levelButtonsParent.SetActive(true);
    }

    /// <summary>Coroutine to load levels after settings have been loaded.</summary>
    private IEnumerator LoadLevelsCoroutine()
    {
        while (Settings.instance == null || !Settings.instance.loaded)
        {
            yield return new WaitForSeconds(0.01f);
        }
        LoadLevels();
    }

    /// <summary>Loads all levels' data and updates their buttons accordingly.</summary>
    private async void LoadLevels()
    {
        loading = true;

        foreach (Button button in levelSelectMenu.GetComponentsInChildren<Button>())
        {
            if (button.gameObject == null || !button.name.Contains("Level"))
                continue;

            int world = int.Parse(button.transform.parent.name.Substring(6, 1));
            int levelNum = int.Parse(button.name.Substring(6, 1));

            if (!button.TryGetComponent<Outline>(out Outline outline)) outline = button.gameObject.AddComponent<Outline>();

            Level level = levels[world - 1, levelNum - 1];
            GameObject star1 = Instantiate(star1Decoration, button.transform);
            GameObject star2 = Instantiate(star2Decoration, button.transform);
            GameObject star3 = Instantiate(star3Decoration, button.transform);
            
            LevelButton lb = new LevelButton(button, level, star1, star2, star3, outline);
            levelButtons.Add(level, lb);
            button.onClick.AddListener(() => LevelSelect.instance.SelectLevel(level));
        }

        // Load all level button data in parallel
        List<Task> loadTasks = new List<Task>();
        foreach (Level level in levels)
        {
            loadTasks.Add(LoadLevelTimes(level));
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
            loadTasks.Add(LoadLevelTimes(level));
        }

        await Task.WhenAll(loadTasks); // Wait for all level data to load
    }

    /// <summary>Sets up a button (color and time) for a level.</summary>
    /// <param name="level">Level to get the info/times of.</param>
    private async Task LoadLevelTimes(Level level)
    {
        string levelTitle = "W" + level.world + "L" + level.level;

        if (!levelButtons.TryGetValue(level, out LevelButton levelButton))
        {
            Debug.LogError($"Level button for {levelTitle} not found.");
            return;
        }

        await level.GetSavedValues();

        SetButtonColors(levelButton);
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
            SetButtonColors(levelButtons[level]);
        }
        loading = false;
    }

    /// <summary>
    /// Sets the colors of the button for each level in the menu based on the
    /// user's completion times compared to the best.
    /// </summary>
    /// <param name="lb">The button to change the color of.</param>
    private void SetButtonColors(LevelButton lb)
    {
        // ColorBlock buttonColorBlock = new ColorBlock();
        ColorBlock trophyColorBlock = new ColorBlock();
        // buttonColorBlock.colorMultiplier = 1;
        trophyColorBlock.colorMultiplier = 1;
        Color textColor = Color.black;
        Level l = lb.level;
        Button b = lb.button;
        // if unbeaten
        // set outline to white
        // else
        // set outline to color respective to how many stars
        // Also, call this method whenever levelSelect is opened
            switch (StarTiers.GetStarTier(l, l.bestTime))
            {
                case -1:
                    lb.outline.effectColor = unbeatenColor;
                    lb.star1.SetActive(false);
                    lb.star2.SetActive(false);
                    lb.star3.SetActive(false);
                    break;
                case 0:
                    lb.outline.effectColor = grayTierColor;
                    lb.star1.SetActive(false);
                    lb.star2.SetActive(false);
                    lb.star3.SetActive(false);
                    break;
                case 1:
                    lb.outline.effectColor = purpleTierColor;
                    lb.star1.SetActive(true);
                    lb.star2.SetActive(false);
                    lb.star3.SetActive(false);
                    break;
                case 2:
                    lb.outline.effectColor = blueTierColor;
                    lb.star1.SetActive(false);
                    lb.star2.SetActive(true);
                    lb.star3.SetActive(false);
                    break;
                case 3:
                    lb.outline.effectColor = greenTierColor;
                    lb.star1.SetActive(false);
                    lb.star2.SetActive(false);
                    lb.star3.SetActive(true);
                    break;
                default:
                    lb.outline.effectColor = Color.black;
                    break;
        }
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

public class LevelButton
{
    public Button button;
    public Outline outline;

    public Level level;

    public GameObject star1;
    public GameObject star2;
    public GameObject star3;

    public LevelButton(Button button, Level level, GameObject star1Decoration,
                       GameObject star2Decoration, GameObject star3Decoration, Outline outline)
    {
        this.button = button;
        this.level = level;
        this.star1 = star1Decoration;
        this.star2 = star2Decoration;
        this.star3 = star3Decoration;
        this.outline = outline;
    }
}
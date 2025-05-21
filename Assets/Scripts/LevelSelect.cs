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

    private IEnumerator LoadLevelsCoroutine()
    {
        while (Settings.instance == null || !Settings.instance.loaded)
        {
            yield return new WaitForSeconds(0.01f);
        }
        LoadLevels();
        // titleOrLoadingText.text = "Level Select";
    }

    async private void LoadLevels()
    {
        loading = true;
        // titleOrLoadingText.text = "Loading Levels...";

        string levelTitle = "";
        foreach (Button levelButton in levelSelectMenu.GetComponentsInChildren<Button>())
        {
            if (levelButton.gameObject == null || !levelButton.name.Contains("Level"))
            {
                continue;
            }
            // Debug.Log(levelButton.name);
            int world = int.Parse(levelButton.transform.parent.name.Substring(6, 1));
            int levelNum = int.Parse(levelButton.name.Substring(6, 1));

            Level level = levels[world - 1, levelNum - 1];
            levelButtons.Add(level, levelButton);
                // levelButton.GetComponentInChildren<TextMeshProUGUI>().text = "Level " + levelNum + Environment.NewLine
                // + Environment.NewLine + level.bestTime.ToString("F2") + "s";

            Button trophyButton = Instantiate(leaderboardEnableButton, levelButton.transform.position + new Vector3(35, 35, 0),
                    Quaternion.identity, levelButton.transform).GetComponent<Button>();
            trophyButton.transform.localScale = new Vector3(1, 1, 1);
            trophyButton.onClick.AddListener(() => Leaderboard.instance.ShowLeaderboard(level));
            levelButton.onClick.AddListener(() => LoadLevel(level.ToString()));
        }

        // titleOrLoadingText.text = "Loading Level Times...";

        foreach (Level level in levels)
        {
            levelTitle = "W" + level.world + "L" + level.level;
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{levelTitle});
            if (playerData.TryGetValue(levelTitle, out var levelTime))
            {
                level.bestTime = levelTime.Value.GetAs<float>();
                level.beaten = true;
                Button levelButton = levelButtons[level];
                levelButton.GetComponentInChildren<TextMeshProUGUI>().text
                  = "Level " + level.level + Environment.NewLine
                + Environment.NewLine + level.bestTime.ToString("F2") + "s";
                
                SetButtonColors(level, levelButton);
            }
        }
        // titleOrLoadingText.text = "Level Select";
        loading = false;
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
        ColorBlock colorBlock = new ColorBlock();
        colorBlock.colorMultiplier = 1;
        Color textColor = Color.black;
        if (!level.beaten)
        {
            colorBlock.normalColor = unbeatenColorset.normalColor;
            colorBlock.highlightedColor = unbeatenColorset.highlightedColor;
            colorBlock.pressedColor = unbeatenColorset.pressedColor;
            colorBlock.selectedColor = unbeatenColorset.selectedColor;
            textColor = unbeatenColorset.textColor;
            Debug.Log("Setting " + level.ToString() + " to unbeaten colors");
        }
        else
        {
            LeaderboardEntry worldRecord = await Leaderboard.instance.GetWorldRecord(level);
            if (!Settings.instance.participateInLeaderboard || level.bestTime - 20 > worldRecord.Time)
            {
                colorBlock.normalColor = whiteTierColorset.normalColor;
                colorBlock.highlightedColor = whiteTierColorset.highlightedColor;
                colorBlock.pressedColor = whiteTierColorset.pressedColor;
                colorBlock.selectedColor = whiteTierColorset.selectedColor;
                textColor = whiteTierColorset.textColor;
                Debug.Log("Setting " + level.ToString() + " to white colors");
            }
            else if (level.bestTime - 10 > worldRecord.Time)
            {
                colorBlock.normalColor = bronzeTierColorset.normalColor;
                colorBlock.highlightedColor = bronzeTierColorset.highlightedColor;
                colorBlock.pressedColor = bronzeTierColorset.pressedColor;
                colorBlock.selectedColor = bronzeTierColorset.selectedColor;
                textColor = bronzeTierColorset.textColor;
                Debug.Log("Setting " + level.ToString() + " to bronze colors");
            }
            else if (level.bestTime - 3 > worldRecord.Time)
            {
                colorBlock.normalColor = silverTierColorset.normalColor;
                colorBlock.highlightedColor = silverTierColorset.highlightedColor;
                colorBlock.pressedColor = silverTierColorset.pressedColor;
                colorBlock.selectedColor = silverTierColorset.selectedColor;
                textColor = silverTierColorset.textColor;
                Debug.Log("Setting " + level.ToString() + " to silver colors");
            }
            else if (level.bestTime - 1 > worldRecord.Time)
            {
                colorBlock.normalColor = goldTierColorset.normalColor;
                colorBlock.highlightedColor = goldTierColorset.highlightedColor;
                colorBlock.pressedColor = goldTierColorset.pressedColor;
                colorBlock.selectedColor = goldTierColorset.selectedColor;
                textColor = goldTierColorset.textColor;
                Debug.Log("Setting " + level.ToString() + " to gold colors");
            }
            else // if (level.bestTime - 0 <= worldRecord.Time)
            {
                colorBlock.normalColor = purpleTierColorset.normalColor;
                colorBlock.highlightedColor = purpleTierColorset.highlightedColor;
                colorBlock.pressedColor = purpleTierColorset.pressedColor;
                colorBlock.selectedColor = purpleTierColorset.selectedColor;
                textColor = purpleTierColorset.textColor;
            Debug.Log("Setting " + level.ToString() + " to purple colors");
            }
        }
        levelButton.colors = colorBlock;
        levelButton.GetComponentInChildren<TextMeshProUGUI>().color = textColor;

    }

    public Level GetNextLevel()
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
}
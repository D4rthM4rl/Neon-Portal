using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using TMPro;
using System;

public class LevelSelect : MonoBehaviour
{
    public static LevelSelect instance;
    public GameObject levelSelectMenu;
    public List<Level> levelsToReload = new List<Level>();

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

    public Dictionary<Level, Button> levelButtons = new Dictionary<Level, Button>();

    private void Start() {
        if (instance == null)
        {
            instance = this;
        }
        else
            Destroy(gameObject);
        StartCoroutine(LoadLevelsCoroutine());
    }

    private IEnumerator LoadLevelsCoroutine()
    {
        while (Settings.instance == null || !Settings.instance.loaded)
        {
            yield return new WaitForSeconds(0.1f);
        }
        LoadLevels();
    }

    async private void LoadLevels()
    {
        string levelTitle = "";
        foreach (Level level in levels)
        {
            levelTitle = "W" + level.world + "L" + level.level;
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{levelTitle});
            if (playerData.TryGetValue(levelTitle, out var levelTime))
            {
                level.bestTime = levelTime.Value.GetAs<float>();
                level.beaten = true;
            }
        }

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
            if (level.beaten)
            {
                levelButton.GetComponentInChildren<TextMeshProUGUI>().text = "Level " + levelNum + Environment.NewLine
                + Environment.NewLine + level.bestTime.ToString("F2") + "s";
            }
            else
            {
                levelButton.GetComponentInChildren<TextMeshProUGUI>().text = "Level " + levelNum;
            }
            levelButton.onClick.AddListener(() => LoadLevel("W" + world + "L" + levelNum));
        }
    }

    public void ReloadLevelTime(Level level)
    {
        float time = level.bestTime;
        level = levels[level.world - 1, level.level - 1];
        Debug.Log("Adjusting level time for " + level.world + " " + level.level);
        if (levelButtons.ContainsKey(level))
        {
            levelButtons[level].GetComponentInChildren<TextMeshProUGUI>().text = "Level " + level.level + Environment.NewLine
            + Environment.NewLine + time.ToString("F2") + "s";
        }
    }

    public Level GetNextLevel()
    {
        foreach (Level level in levels)
        {
            if (!level.beaten)
            {
                return level;
            }
        }
        return null;
    }

    public void LoadLevel(Level level)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("W" + level.world + "L" + level.level);
        if (Settings.instance.showTimer) Timer.instance.timerText.enabled = true;
        else Timer.instance.timerText.enabled = false;
        gameObject.SetActive(false);
        // StartCoroutine(WaitForRemovePlayer());
    }

    public void LoadLevel(string level)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(level);
        gameObject.SetActive(false);
        if (Settings.instance.showTimer) Timer.instance.timerText.enabled = true;
        else Timer.instance.timerText.enabled = false;
        // StartCoroutine(WaitForRemovePlayer());
    }

    public IEnumerator WaitForRemovePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return new WaitForSeconds(0.1f);
        }
        Destroy(player);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LevelSelect : MonoBehaviour
{
    public static LevelSelect instance;
    public GameObject levelSelectMenu;

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

    private void Awake() {
        if (instance == null)
        {
            instance = this;
        }
        else
            Destroy(gameObject);


        foreach (Level level in levels)
        {
            if (PlayerPrefs.HasKey("W" + level.world + "L" + level.level))
            {
                level.bestTime = PlayerPrefs.GetFloat("W" + level.world + "L" + level.level);
                level.beaten = true;
            }
        }

        foreach (Button levelButton in levelSelectMenu.GetComponentsInChildren<Button>())
        {
            if (!levelButton.name.Contains("Level"))
            {
                continue;
            }
            // Debug.Log(levelButton.name);
            int world = int.Parse(levelButton.transform.parent.name.Substring(6, 1));
            int levelNum = int.Parse(levelButton.name.Substring(6, 1));

            Level level = levels[world - 1, levelNum - 1];
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
    }

    public void LoadLevel(string level)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(level);
    }
}
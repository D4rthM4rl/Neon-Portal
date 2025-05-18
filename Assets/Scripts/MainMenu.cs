using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // public Image sunrise1;
    [SerializeField]
    private GameObject background;
    [SerializeField]
    private GameObject mainMenuUI;
    [SerializeField]
    private GameObject levelSelectUI;
    [SerializeField]
    private GameObject optionsUI;

    public void Play()
    {
        Level nextLevel = LevelSelect.instance.GetNextLevel();
        if (nextLevel == null) 
            OpenLevelSelect();
        else 
            LevelSelect.instance.LoadLevel(LevelSelect.instance.GetNextLevel());
    }

    public void OpenLevelSelect()
    {
        mainMenuUI.SetActive(false);
        levelSelectUI.SetActive(true);
        optionsUI.SetActive(false);
    }

    public void OpenOptions()
    {
        mainMenuUI.SetActive(false);
        levelSelectUI.SetActive(false);
        optionsUI.SetActive(true);
    }

    public void Quit()
    {
        Application.Quit();
    }
}

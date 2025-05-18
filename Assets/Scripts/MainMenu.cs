using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class MainMenu : MonoBehaviour
{
    public Sprite sunrise1;
    [SerializeField]
    private GameObject background;
    [SerializeField]
    private GameObject title;
    [SerializeField]
    private GameObject mainMenuUI;
    [SerializeField]
    private GameObject levelSelectUI;
    [SerializeField]
    private GameObject optionsUI;

    public static bool rotateCameraWithGravity = true;

    async void Start()
    {
        await UnityServices.InitializeAsync();
    }

    public void OptOut()
    {
        AnalyticsService.Instance.StopDataCollection();
    }

    public void OptIn()
    {
        AnalyticsService.Instance.StartDataCollection();
    }

    public void RequestDataDelection()
    {
        AnalyticsService.Instance.RequestDataDeletion();
    }

    public void Play()
    {
        Level nextLevel = LevelSelect.instance.GetNextLevel();
        if (nextLevel == null) 
            OpenLevelSelect();
        else 
            LevelSelect.instance.LoadLevel(LevelSelect.instance.GetNextLevel());
    }

    public void OpenMainMenu()
    {
        mainMenuUI.SetActive(true);
        levelSelectUI.SetActive(false);
        optionsUI.SetActive(false);
        title.SetActive(true);
    }

    public void OpenLevelSelect()
    {
        mainMenuUI.SetActive(false);
        levelSelectUI.SetActive(true);
        optionsUI.SetActive(false);
        title.SetActive(false);
    }

    public void OpenOptions()
    {
        mainMenuUI.SetActive(false);
        levelSelectUI.SetActive(false);
        optionsUI.SetActive(true);
        title.SetActive(false);
    }

    public void Quit()
    {
        Application.Quit();
    }
}

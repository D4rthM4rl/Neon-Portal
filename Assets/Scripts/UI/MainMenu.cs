using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using TMPro;

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
    [SerializeField]
    private GameObject optButton;

    public static MainMenu instance;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            instance.gameObject.SetActive(true);
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        StartCoroutine(LoadOptButton());
    }

    private IEnumerator LoadOptButton()
    {
        while (Settings.instance == null || !Settings.instance.loaded)
        {
            yield return new WaitForSeconds(0.01f);
        }
        optButton.GetComponentInChildren<TextMeshProUGUI>().text = !Settings.instance.optedIn ? "Opt In" : "Opt Out";
    }

    public void ToggleOpt()
    {
        if (optButton.GetComponentInChildren<TextMeshProUGUI>().text == "Opt In")
        {
            OptIn();
        }
        else
        {
            OptOut();
        }
    }

    async public void OptOut()
    {
        Settings.instance.optedIn = false;
        Debug.Log("Opted out of analytics");

        var choice = new Dictionary<string, object>{ { "AnalyticsOptChoice", "Opt Out" } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(choice);
        AnalyticsService.Instance.StopDataCollection();

        optButton.GetComponentInChildren<TextMeshProUGUI>().text = "Opt In";
    }

    async public void OptIn()
    {
        Settings.instance.optedIn = true;
        Debug.Log("Opted in to analytics");

        var choice = new Dictionary<string, object>{ { "AnalyticsOptChoice", "Opt In" } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(choice);
        AnalyticsService.Instance.StartDataCollection();

        optButton.GetComponentInChildren<TextMeshProUGUI>().text = "Opt Out";
    }

    public void RequestDataDelection()
    {
        AnalyticsService.Instance.RequestDataDeletion();
    }

    public void Play()
    {
        Timer.instance.ResetInactivityTimer();
        if (LevelSelect.instance.loading)
            return;
        Level nextLevel = LevelSelect.instance.GetNextLevel();
        if (nextLevel == null) 
            OpenLevelSelect();
        else 
        {
            gameObject.SetActive(false);
            LevelSelect.instance.LoadLevel(nextLevel);
        }
    }

    public void OpenMainMenu()
    {
        Timer.instance.ResetInactivityTimer();
        mainMenuUI.SetActive(true);
        levelSelectUI.SetActive(false);
        optionsUI.SetActive(false);
        title.SetActive(true);
    }

    public void OpenLevelSelect()
    {
        Timer.instance.ResetInactivityTimer();
        mainMenuUI.SetActive(false);
        levelSelectUI.SetActive(true);
        optionsUI.SetActive(false);
        title.SetActive(false);
        // LevelSelect.instance.titleOrLoadingText.text = "Loading Level Times...";
        foreach (Level level in LevelSelect.instance.levelsToReload)
        {
            LevelSelect.instance.ReloadLevelTime(level);
        }
        // LevelSelect.instance.titleOrLoadingText.text = "Level Select";
        LevelSelect.instance.levelsToReload.Clear();
    }

    public void OpenOptions()
    {
        Timer.instance.ResetInactivityTimer();
        mainMenuUI.SetActive(false);
        levelSelectUI.SetActive(false);
        optionsUI.SetActive(true);
        title.SetActive(false);
        Settings.instance.SetPortalButtonColors();
        Settings.instance.playerNameInput.GetComponent<TMP_InputField>().text = Settings.instance.playerLeaderboardName;
    }
    
    public void Quit()
    {
        Application.Quit();
    }
}

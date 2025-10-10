using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    /// <summary>The GameObject that contains the background image.</summary>
    [SerializeField] private GameObject background;
    /// <summary>The GameObject that contains the title text.</summary>
    [SerializeField] private GameObject title;
    /// <summary>The GameObject parent for the UI for the main menu.</summary>
    [SerializeField] private GameObject mainMenuUI;
    /// <summary>The GameObject for Play/every level completed button.</summary>
    [SerializeField] private Button playButton;
    /// <summary>The GameObject for Play/every level completed button.</summary>
    [SerializeField] private TextMeshProUGUI playButtonText;
    /// <summary>The GameObject parent for the UI for the level select menu.</summary>
    [SerializeField] private GameObject levelSelectUI;
    /// <summary>The GameObject parent for the UI for the options menu.</summary>
    [SerializeField] private GameObject optionsUI;
    /// <summary>The GameObject that contains the opt in/out button.</summary>
    [SerializeField] private GameObject optButton;
    /// <summary>Singleton instance for the Main menu</summary>
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
    }

    private void Start() {
        StartCoroutine(SetPlayButton());
    }

    /// <summary>
    /// Toggles what the opt button says and does based on whether currently
    /// opted in or out.
    /// </summary>
    public void ToggleOpt()
    {
        if (!OnlineServices.online)
        {
            Debug.Log("Offline, can't toggle Opt status");
            return;
        }

        if (optButton.GetComponentInChildren<TextMeshProUGUI>().text == "Opt In")
        {
            OptIn();
        }
        else
        {
            OptOut();
        }
    }
    
    /// <summary>Opt out of game data collection.</summary>
    public void OptOut()
    {
        Settings.instance.optedIn = false;
        Debug.Log("Opted out of analytics");

        // var choice = new Dictionary<string, object>{ { "AnalyticsOptChoice", "Opt Out" } };
        // await CloudSaveService.Instance.Data.Player.SaveAsync(choice);
        OnlineServices.ChangeDataCollection(false);
        string optLabel;
        if (OnlineServices.online) optLabel = "Opt In";
        else optLabel = "Opted in (Offline)";
        optButton.GetComponentInChildren<TextMeshProUGUI>().text = optLabel;
    }

    /// <summary>Opt into game data collection.</summary>
    public void OptIn()
    {
        Settings.instance.optedIn = true;
        Debug.Log("Opted in to analytics");

        // var choice = new Dictionary<string, object>{ { "AnalyticsOptChoice", "Opt In" } };
        // await CloudSaveService.Instance.Data.Player.SaveAsync(choice);
        OnlineServices.ChangeDataCollection(true);
        string optLabel;
        if (OnlineServices.online) optLabel = "Opt Out";
        else optLabel = "Opted out (Offline)";
        optButton.GetComponentInChildren<TextMeshProUGUI>().text = optLabel;
    }

    /// <summary>
    /// Request your data be deleted.
    /// </summary>
    public void RequestDataDelection()
    {
        if (!OnlineServices.online)
        {
            Debug.Log("Offline, can't request data deletion");
            return;
        }
        OnlineServices.RequestDataDeletion();
    }

    /// <summary>Start the unbeaten level or open level select if nothing unbeaten.</summary>
    public void Play()
    {
        Timer.instance.ResetInactivityTimer();
        if (LevelSelect.instance.loading)
            return;
        Level nextLevel = LevelSelect.instance.GetNextUnbeatenLevel();
        if (nextLevel == null) 
            OpenLevelSelect();
        else 
        {
            StartCoroutine(LevelSelect.instance.LoadLevel(nextLevel));
        }
    }

    public void OpenMainMenu()
    {
        Timer.instance.ResetInactivityTimer();
        mainMenuUI.SetActive(true);
        StartCoroutine(SetPlayButton());
        levelSelectUI.SetActive(false);
        optionsUI.SetActive(false);
    }

    public IEnumerator SetPlayButton()
    {
        while (!LevelSelect.instance || LevelSelect.instance.loading) yield return null;
        Level nextLevel = LevelSelect.instance.GetNextUnbeatenLevel();

        if (nextLevel != null)
        {
            playButton.interactable = true;
            playButtonText.text = "Play";   
            playButton.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 250);
        }
        else
        {
            playButton.interactable = false;
            playButtonText.text = "All Levels Completed";
            playButton.GetComponent<RectTransform>().sizeDelta = new Vector2(1850, 250);
        }
    }

    public void OpenLevelSelect()
    {
        Timer.instance.ResetInactivityTimer();
        mainMenuUI.SetActive(false);
        levelSelectUI.SetActive(true);
        LevelSelect.instance.UnselectLevel();
        optionsUI.SetActive(false);
        foreach (Level level in LevelSelect.instance.levelsToReload)
        {
            LevelSelect.instance.ReloadLevelTime(level);
        }
        LevelSelect.instance.levelsToReload.Clear();
    }

    /// <summary>Open the Options menu.</summary>
    public void OpenOptions()
    {
        Timer.instance.ResetInactivityTimer();
        mainMenuUI.SetActive(false);
        levelSelectUI.SetActive(false);
        optionsUI.SetActive(true);
        Settings.instance.MakeSettingsUIMatchSaved();
    }
    
    /// <summary>Tries to quit/close the game.</summary>
    public void Quit()
    {
        Application.Quit();
    }
}

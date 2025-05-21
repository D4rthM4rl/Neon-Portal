using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using TMPro;

public class Settings : MonoBehaviour
{
    public static Settings instance;
    public bool rotateCameraWithGravity = true;
    public bool showTimer = true;

    public bool participateInLeaderboard = true;
    public string playerLeaderboardName = "";
    [SerializeField]
    public TMP_InputField playerNameInput;
    [SerializeField]
    private TextMeshProUGUI playerNameErrorText;


    public bool optedIn = false;
    public bool loaded = false;

    private List<string> badWords = new List<string>
    {
        "nigger",
        "nigga",
        "bitch",
        "fuck",
        "shit",
        "whore",
        "cunt",
        "faggot",
        "dyke",
        "cock",
        "damn",
    };

    async void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        else
        {
            Destroy(gameObject);
        }
        
        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"AnalyticsOptChoice"});
        if (playerData.TryGetValue("AnalyticsOptChoice", out var analyticsOptChoice)) 
        {
            Debug.Log($"AnalyticsOptChoice: {analyticsOptChoice.Value.GetAs<string>()}");
            if (analyticsOptChoice.Value.GetAs<string>() == "Opt In")
            {
                optedIn = true;
                
                AnalyticsService.Instance.StartDataCollection();
            }
            else
            {
                optedIn = false;
                AnalyticsService.Instance.StopDataCollection();
            }
        }
        playerLeaderboardName = await AuthenticationService.Instance.GetPlayerNameAsync();
        // playerNameInput.GetComponent<TMP_InputField>().text = playerLeaderboardName;
        loaded = true;
    }

    public void SetRotateCameraWithGravity(bool value)
    {
        rotateCameraWithGravity = value;
    }
    public void SetTimerVisibility(bool value)
    {
        showTimer = value;
    }

    public void CompletePlayerName()
    {
        string text = playerNameInput.GetComponent<TMP_InputField>().text;
        if (text.Contains(" ") || text.Contains("\"") || text.Contains("{") || text.Contains("}") || text.Contains(","))
        {
            playerNameErrorText.enabled = true;
            playerNameErrorText.text = "No quotes, commas, brackets, or spaces allowed";
            return;
        }
        if (text.Length > 20)
        {
            playerNameErrorText.enabled = true;
            playerNameErrorText.text = "Must be less than 20 characters";
            return;
        }
        foreach (string badWord in badWords)
        {
            if (text.ToLower().Contains(badWord))
            {
                playerNameErrorText.enabled = true;
                playerNameErrorText.text = "Contains bad word";
                return;
            }
        }
        playerNameErrorText.enabled = false;
        playerLeaderboardName = text;
        AuthenticationService.Instance.UpdatePlayerNameAsync(text);
    }
}

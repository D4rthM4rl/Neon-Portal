using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine.UI;
using TMPro;

public class Settings : MonoBehaviour
{
    public static Settings instance;
    public bool rotateCameraWithGravity = true;
    public bool showTimer = true;

    public Color portal1Color;
    [SerializeField]
    private Button portal1ColorButton;
    private bool settingPortal1Color = false;
    public Color portal2Color;
    [SerializeField]
    private Button portal2ColorButton;
    private bool settingPortal2Color = false;
    [SerializeField]
    private ColorPickerControl colorPicker;

    public bool participateInLeaderboard = true;
    public string playerLeaderboardName = "";
    [SerializeField]
    public TMP_InputField playerNameInput;
    [SerializeField]
    private TextMeshProUGUI playerNameErrorText;


    public bool optedIn = true;
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
            if (analyticsOptChoice.Value.GetAs<string>() == "Opt Out")
            {
                optedIn = false;
                AnalyticsService.Instance.StopDataCollection();
            }
            else
            {
                optedIn = true;
                AnalyticsService.Instance.StartDataCollection();
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

    public void SetPortalButtonColors()
    {
        SetButtonColor(portal1Color, portal1ColorButton);
        SetButtonColor(portal2Color, portal2ColorButton);
    }

    public void ChangePortal1Colors()
    {
        settingPortal1Color = true;
        settingPortal2Color = false;
        StopCoroutine(ChangePortal2ColorsAsync());
        colorPicker.transform.parent.gameObject.SetActive(true);
        colorPicker.InitializeWithColor(portal1Color);
        StartCoroutine(ChangePortal1ColorsAsync());
    }

    private IEnumerator ChangePortal1ColorsAsync()
    {
        while (colorPicker.gameObject.activeInHierarchy == true && !settingPortal2Color)
        {
            Color c = colorPicker.pickedColor;
            portal1Color = c;
            SetButtonColor(c, portal1ColorButton);
            
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void ChangePortal2Colors()
    {
        settingPortal2Color = true;
        settingPortal1Color = false;
        StopCoroutine(ChangePortal1ColorsAsync());
        colorPicker.transform.parent.gameObject.SetActive(true);
        colorPicker.InitializeWithColor(portal2Color);
        StartCoroutine(ChangePortal2ColorsAsync());
    }

    private IEnumerator ChangePortal2ColorsAsync()
    {
        while (colorPicker.gameObject.activeInHierarchy == true && !settingPortal1Color)
        {
            Color c = colorPicker.pickedColor;
            portal2Color = c;
            SetButtonColor(c, portal2ColorButton);
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void SetButtonColor(Color c, Button button)
    {
        ColorBlock cb = button.colors;
        cb.normalColor = c;
        cb.highlightedColor = c;
        cb.pressedColor = c;
        cb.selectedColor = c;
        cb.disabledColor = c;
        button.colors = cb;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Settings : MonoBehaviour
{
    public static Settings instance;
    public bool rotateCameraWithGravity = false;
    public bool rotateMobileControls = true;
    public bool leftClickForBothPortals = true;
    public bool needToTouchGroundToReenterPortal = true;
    public bool showTimer = true;
    public bool optedIn = true;
    public PlayerMovementType movement = PlayerMovementType.Normal;

    public Color portal1Color;
    private Color portal1SavedColor;
    private bool settingPortal1Color = false;

    public Color portal2Color;
    private Color portal2SavedColor;
    private bool settingPortal2Color = false;
    [SerializeField] private ColorPickerControl colorPicker;
    [SerializeField] private Image blackOverlay;

    public bool participateInLeaderboard = true;
    public string playerLeaderboardName = "";
    #region Settings UI to Adjust
    [Header("Settings UI Elements to Refresh")]
    
    [SerializeField] private Button optButton;
    [SerializeField] private TMP_Dropdown playerMovementDropdown;
    [SerializeField] private Button portal1ColorButton;
    [SerializeField] private Button portal2ColorButton;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Toggle showTimerToggle;
    [SerializeField] private Toggle portalSplitToggle;
    [SerializeField] private Toggle needToTouchGroundToggle;
    [SerializeField] private Toggle rotateCameraToggle;
    [SerializeField] private Toggle rotateMobileControlsToggle;
    [SerializeField] private Toggle onlineToggle;
    [SerializeField] private TextMeshProUGUI portalSplitText;
    

    #endregion

    [SerializeField] private TextMeshProUGUI playerNameErrorText;

    public PlatformType platform = PlatformType.Computer;
    public bool loaded = false;

    /// <summary>A list of bad words that are not allowed in the player name</summary>
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

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        if (isMobile())
        {
            // If on mobile, set the platform type to Phone
            platform = PlatformType.Phone;
            MainMenu.instance.mobileMainMenuUI.SetActive(true);
            MainMenu.instance.pcMainMenuUI.SetActive(false);
            rotateMobileControlsText.gameObject.SetActive(true);
            portalSplitText.text = "Manually Switch Portal Creation";
            Debug.Log("Running on mobile device");
        }
        else
        {
            // If on computer, set the platform type to Computer
            platform = PlatformType.Computer;
            MainMenu.instance.pcMainMenuUI.SetActive(true);
            MainMenu.instance.mobileMainMenuUI.SetActive(false);
            rotateMobileControlsToggle.gameObject.SetActive(false);
            portalSplitText.text = "Use Left Click for Both Portals";
            Debug.Log("Running on computer");
        }
        
        
        SetSettingsValuesToMatchSaved();

        // playerNameInput.GetComponent<TMP_InputField>().text = playerLeaderboardName;
        loaded = true;
    }

    #if !UNITY_EDITOR && UNITY_WEBGL

        [System.Runtime.InteropServices.DllImport("__Internal")]
        public static extern bool IsMobileBrowser();
      
        [System.Runtime.InteropServices.DllImport("__Internal")]
        public static extern bool IsPreferredDesktopPlatform();
    #else
            private static bool IsMobileBrowser() => false;
            public static bool IsPreferredDesktopPlatform() => true;
    #endif

    /// <summary>Returns if the game is WebGL and running on a mobile device.</summary>
    /// <returns>True if the game is on WebGL on a mobile device.</returns>
    private bool isMobile()
    {
        return IsMobileBrowser();
    }

    #region Saving Helpers

    /// <summary>Makes the settings values in the menu match what is saved.</summary>
    private void SetSettingsValuesToMatchSaved()
    {
        GetSavedOpt();
        GetSavedPortalColors();
        
        GetSavedTimerVisibility();
        GetSavedPlayerMovementType();
        GetSavedRotateCameraWithGravity();
        GetSavedRotateMobileControls();
        GetSavedPortalSplit();
        GetSavedPortalEntering();
    }

    /// <summary>Makes the settings visually match what is saved</summary>
    public void MakeSettingsUIMatchSaved()
    {
        if (OnlineServices.online)
        {
            optButton.GetComponentInChildren<TextMeshProUGUI>().text = !optedIn ? "Opt In" : "Opt Out";

        }
        else
        {
            optButton.GetComponentInChildren<TextMeshProUGUI>().text = "Opted Out (Offline)";
        }
        Debug.Assert(portal1ColorButton != null, "Portal 1 color button is null");
        GetSavedPortalColors(true);
        playerNameInput.GetComponent<TMP_InputField>().text = playerLeaderboardName;
        playerMovementDropdown.value = (int)movement;

        showTimerToggle.isOn = showTimer;
        rotateCameraToggle.isOn = rotateCameraWithGravity;
        // NOT TRIGGERING WHEN IT SHOULD BE
        // Debug.Log("Rotate camera: " + rotateCameraWithGravity);
        if (rotateCameraWithGravity) SetInteractableRotateMobileControls(false);
        else rotateMobileControlsToggle.isOn = rotateMobileControls;
        onlineToggle.isOn = OnlineServices.online;
        portalSplitToggle.isOn = leftClickForBothPortals;
        needToTouchGroundToggle.isOn = needToTouchGroundToReenterPortal;
    }
    
    /// <summary>Submits the player name from the textbox.</summary>
    public void CompletePlayerName()
    {
        string text = playerNameInput.GetComponent<TMP_InputField>().text;
        if (!OnlineServices.online)
        {
            playerNameErrorText.enabled = true;
            playerNameErrorText.text = "Offline, leaderboard is disabled";
            return;
        }
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
        OnlineServices.UpdatePlayerName(text);
    }

    /// <summary>Sets the color for Portal 1 to what was chosen in the color picker.</summary>
    public void ChangePortal1Colors()
    {
        settingPortal1Color = true;
        settingPortal2Color = false;
        StopCoroutine(ChangePortal2ColorsAsync());
        colorPicker.transform.parent.gameObject.SetActive(true);
        colorPicker.InitializeWithColor(portal1Color);
        StartCoroutine(ChangePortal1ColorsAsync());
    }

    /// <summary>
    /// Waits until it can change the Portal 1 color then sets the color picker button
    /// to correspond to picked color.
    /// </summary>
    private IEnumerator ChangePortal1ColorsAsync()
    {
        while (colorPicker.gameObject.activeInHierarchy == true && !settingPortal2Color)
        {
            Color c = colorPicker.pickedColor;
            portal1Color = c;
            SetButtonColor(c, portal1ColorButton);
            
            // StartCoroutine(SavePortalColor("Portal1Color", portal1Color));
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>Sets the color for Portal 2 to what was chosen in the color picker.</summary>
    public void ChangePortal2Colors()
    {
        settingPortal2Color = true;
        settingPortal1Color = false;
        StopCoroutine(ChangePortal1ColorsAsync());
        colorPicker.transform.parent.gameObject.SetActive(true);
        colorPicker.InitializeWithColor(portal2Color);
        StartCoroutine(ChangePortal2ColorsAsync());
    }

    /// <summary>
    /// Waits until it can change the Portal 2 color then sets the color picker button
    /// to correspond to picked color.
    /// </summary>
    private IEnumerator ChangePortal2ColorsAsync()
    {
        while (colorPicker.gameObject.activeInHierarchy == true && !settingPortal1Color)
        {
            Color c = colorPicker.pickedColor;
            portal2Color = c;
            SetButtonColor(c, portal2ColorButton);

            // StartCoroutine(SavePortalColor("Portal2Color", portal1Color));

            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion

    #region Settings Getters (PlayerPrefs)

    /// <summary>
    /// Sets the data collection based on what is saved.
    /// </summary>
    private void GetSavedOpt()
    {
        optedIn = PlayerPrefs.GetInt("AnalyticsOptChoice", 1) == 1;
        OnlineServices.ChangeDataCollection(optedIn);
    }

    private void GetSavedTimerVisibility()
    {
        showTimer = PlayerPrefs.GetInt("ShowTimer", 1) == 1;
    }

    private void GetSavedPlayerMovementType()
    {
        int value = PlayerPrefs.GetInt("PlayerMovementType", 0);
        movement = (PlayerMovementType)value;
        playerMovementDropdown.value = value;
    }

    private void GetSavedRotateCameraWithGravity()
    {
        rotateCameraWithGravity = PlayerPrefs.GetInt("RotateCameraWithGravity", 0) == 1;
    }

    private void GetSavedRotateMobileControls()
    {
        rotateMobileControls = PlayerPrefs.GetInt("RotateMobileCamera", 0) == 1;
    }

    private void GetSavedPortalSplit()
    {
        leftClickForBothPortals = PlayerPrefs.GetInt("LeftClickForBothPortals", 1) == 1;
    }

    private void GetSavedPortalEntering()
    {
        needToTouchGroundToReenterPortal = PlayerPrefs.GetInt("NeedToTouchGroundToReenterPortal", 1) == 1;
    }

    public void GetSavedPortalColors(bool setButtonColors = false)
    {
        if (ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString("Portal1Color", "FF00FF"), out portal1Color) == false)
            portal1Color = Color.magenta;

        if (ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString("Portal2Color", "00FFFF"), out portal2Color) == false)
            portal2Color = Color.cyan;

        if (setButtonColors)
        {
            SetButtonColor(portal1Color, portal1ColorButton);
            SetButtonColor(portal2Color, portal2ColorButton);
        }
    }

    #endregion
    
    #region Settings Getters (Unity Cloud)

    // async private void GetSavedOpt()
    // {
    //     var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"AnalyticsOptChoice"});
    //     if (playerData.TryGetValue("AnalyticsOptChoice", out var analyticsOptChoice)) 
    //     {
    //         if (analyticsOptChoice.Value.GetAs<string>() == "Opt Out")
    //         {
    //             optedIn = false;
    //             AnalyticsService.Instance.StopDataCollection();
    //         }
    //         else
    //         {
    //             optedIn = true;
    //             AnalyticsService.Instance.StartDataCollection();
    //         }
    //     }
    //     else 
    //     {
    //         // If the key doesn't exist, default to opted in
    //         optedIn = true;
    //         AnalyticsService.Instance.StartDataCollection();
    //     }
    // }

    // async private void GetSavedTimerVisibility()
    // {
    //     var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"ShowTimer"});
    //     if (playerData.TryGetValue("ShowTimer", out var choice)) 
    //     {
    //         if (choice.Value.GetAs<bool>())
    //         {
    //             showTimer = true;
    //         }
    //         else
    //         {
    //             showTimer = false;
    //         }
    //     }
    //     else
    //     {
    //         // If the key doesn't exist, default to true
    //         showTimer = true;
    //     }
    // }

    // async private void GetSavedPlayerMovementType()
    // {
    //     var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"PlayerMovementType"});
    //     if (playerData.TryGetValue("PlayerMovementType", out var playerMovementType))
    //     {
    //         movement = (PlayerMovementType)playerMovementType.Value.GetAs<int>();
    //         playerMovementDropdown.value = (int)movement;
    //     }
    //     else
    //     {
    //         // If the key doesn't exist, generate a random choice
    //         movement = (PlayerMovementType)Random.Range(0, 2);
    //         var saveMovement = new Dictionary<string, object> { { "PlayerMovementType", (int)movement } };
    //         await CloudSaveService.Instance.Data.Player.SaveAsync(saveMovement);
    //         playerMovementDropdown.value = (int)movement;
    //     }
    // }

    // async private void GetSavedRotateCameraWithGravity()
    // {
    //     var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"RotateCameraWithGravity"});
    //     if (playerData.TryGetValue("RotateCameraWithGravity", out var choice)) 
    //     {
    //         if (choice.Value.GetAs<bool>())
    //         {
    //             rotateCameraWithGravity = true;
    //         }
    //         else
    //         {
    //             rotateCameraWithGravity = false;
    //         }
    //     }
    //     else
    //     {
    //         // If the key doesn't exist, default to true
    //         rotateCameraWithGravity = true;
    //     }
    // }

    // async private void GetSavedPortalSplit()
    // {
    //     var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"LeftClickForBothPortals"});
    //     if (playerData.TryGetValue("LeftClickForBothPortals", out var choice)) 
    //     {
    //         if (choice.Value.GetAs<bool>())
    //         {
    //             leftClickForBothPortals = true;
    //         }
    //         else
    //         {
    //             leftClickForBothPortals = false;
    //         }
    //     }
    //     else
    //     {
    //         // If the key doesn't exist, default to true
    //         leftClickForBothPortals = true;
    //     }
    // }

    // async private void GetSavedPortalEntering()
    // {
    //     var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"NeedToTouchGroundToReenterPortal"});
    //     if (playerData.TryGetValue("NeedToTouchGroundToReenterPortal", out var choice)) 
    //     {
    //         if (choice.Value.GetAs<bool>())
    //         {
    //             needToTouchGroundToReenterPortal = true;
    //         }
    //         else
    //         {
    //             needToTouchGroundToReenterPortal = false;
    //         }
    //     }
    //     else
    //     {
    //         // If the key doesn't exist, default to true
    //         needToTouchGroundToReenterPortal = true;
    //     }
    // }

    #endregion

    #region Settings Savers (PlayerPrefs)

    public async void ToggleOnline()
    {
        if (!onlineToggle.isOn)
        {
            OnlineServices.online = false;
        }
        else
        {
            await OnlineServices.TryToGoOnline();
            if (!OnlineServices.online) ErrorOnline();
            onlineToggle.isOn = OnlineServices.online;
        }
    }

    public void ErrorOnline()
    {
        StartCoroutine(ErrorOnlineCoroutine());
    }

    private IEnumerator ErrorOnlineCoroutine()
    {
        onlineToggle.isOn = false;
        TextMeshProUGUI onlineText = onlineToggle.GetComponentInChildren<TextMeshProUGUI>();
        onlineText.text = "Error Going Online";
        onlineText.color = new Color(.6f, 0, 0);
        yield return new WaitForSeconds(3);
        onlineText.text = "Online";
        onlineText.color = Color.black;
    }

    public void SetRotateCameraWithGravity()
    {
        rotateCameraWithGravity = rotateCameraToggle.isOn;
        SetInteractableRotateMobileControls(!rotateCameraWithGravity);
        PlayerPrefs.SetInt("RotateCameraWithGravity", rotateCameraWithGravity ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetRotateMobileControls()
    {
        rotateMobileControls = rotateMobileControlsToggle.isOn;
        PlayerPrefs.SetInt("RotateMobileControls", rotateMobileControls ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetPortalsSplit()
    {
        leftClickForBothPortals = portalSplitToggle.isOn;
        PlayerPrefs.SetInt("LeftClickForBothPortals", leftClickForBothPortals ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetPortalEntering()
    {
        needToTouchGroundToReenterPortal = needToTouchGroundToggle.isOn;
        PlayerPrefs.SetInt("NeedToTouchGroundToReenterPortal", needToTouchGroundToReenterPortal ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetTimerVisibility()
    {
        showTimer = showTimerToggle.isOn;
        PlayerPrefs.SetInt("ShowTimer", showTimer ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetPlayerMovement()
    {
        movement = (PlayerMovementType)playerMovementDropdown.value;
        PlayerPrefs.SetInt("PlayerMovementType", playerMovementDropdown.value);
        PlayerPrefs.Save();
    }

    public void SavePortalColors()
    {
        PlayerPrefs.SetString("Portal1Color", ColorUtility.ToHtmlStringRGB(portal1Color));
        PlayerPrefs.SetString("Portal2Color", ColorUtility.ToHtmlStringRGB(portal2Color));
        PlayerPrefs.Save();
    }

    #endregion

    #region Settings Savers (Unity Cloud)

    // async public void SetRotateCameraWithGravity()
    // {
    //     rotateCameraWithGravity = rotateCameraToggle.isOn;
    //     var result = new Dictionary<string, object> { { "RotateCameraWithGravity", rotateCameraWithGravity } };
    //     await CloudSaveService.Instance.Data.Player.SaveAsync(result);
    // }

    // async public void SetPortalsSplit()
    // {
    //     leftClickForBothPortals = portalSplitToggle.isOn;
    //     var result = new Dictionary<string, object> { { "LeftClickForBothPortals", leftClickForBothPortals } };
    //     await CloudSaveService.Instance.Data.Player.SaveAsync(result);
    // }

    // async public void SetPortalEntering()
    // {
    //     needToTouchGroundToReenterPortal = needToTouchGroundToggle.isOn;
    //     var result = new Dictionary<string, object> { { "NeedToTouchGroundToReenterPortal", needToTouchGroundToReenterPortal } };
    //     await CloudSaveService.Instance.Data.Player.SaveAsync(result);
    // }
    
    // async public void SetTimerVisibility()
    // {
    //     showTimer = showTimerToggle.isOn;
    //     var result = new Dictionary<string, object> { { "ShowTimer", showTimer } };
    //     await CloudSaveService.Instance.Data.Player.SaveAsync(result);
    // }
    
    // async public void SetPlayerMovement()
    // {
    //     movement = (PlayerMovementType)playerMovementDropdown.value;
    //     var saveMovement = new Dictionary<string, object> { { "PlayerMovementType", playerMovementDropdown.value } };
    //     await CloudSaveService.Instance.Data.Player.SaveAsync(saveMovement);
    // }

    // async public void GetSavedPortalColors(bool setButtonColors = false)
    // {
    //     var portal1Data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"Portal1Color"});
    //     if (portal1Data.TryGetValue("Portal1Color", out var portal1ColorValue))
    //     {
    //         string portal1ColorHex = "#" + portal1ColorValue.Value.GetAs<string>();
    //         Debug.Assert(ColorUtility.TryParseHtmlString(portal1ColorHex, out portal1Color),
    //             "Couldn't parse saved portal 1 color");
    //     }
    //     var portal2Data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"Portal2Color"});
    //     if (portal2Data.TryGetValue("Portal2Color", out var portal2ColorValue))
    //     {
    //         string portal2ColorHex = "#" + portal2ColorValue.Value.GetAs<string>();
    //         Debug.Assert(ColorUtility.TryParseHtmlString(portal2ColorHex, out portal2Color), 
    //             "Couldn't parse saved portal 2 color");
    //     }
    //     if (setButtonColors)
    //     {
    //         SetButtonColor(portal1Color, portal1ColorButton);
    //         SetButtonColor(portal2Color, portal2ColorButton);
    //     }
    // }

    #endregion

    [SerializeField] private TextMeshProUGUI rotateMobileControlsText;
    private void SetInteractableRotateMobileControls(bool interactable)
    {
        rotateMobileControlsToggle.isOn = false;
        rotateMobileControlsToggle.interactable = interactable;
        Color c = rotateMobileControlsText.color;
        c.a = interactable ? 1 : .25f;
        rotateMobileControlsText.color = c;
        SetRotateMobileControls();
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

/// <summary>
/// What movement type to use for the player. <br/>
/// Quick: Original movement which has quicker acceleration. <br/>
/// Normal: Later added movement which has slower acceleration.
/// </summary>
public enum PlayerMovementType
{
    Quick,
    Normal,
}

/// <summary>
/// What platform the user is on. <br/>
/// Phone: any mobile device including iPad. <br/>
/// Computer: any kind of laptop/desktop.
/// </summary>
public enum PlatformType
{
    Phone,
    Computer
}

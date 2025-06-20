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
    [SerializeField]
    private ColorPickerControl colorPicker;

    public bool participateInLeaderboard = true;
    public string playerLeaderboardName = "";
    #region Settings UI to Adjust
    [Header("Settings UI Elements to Refresh")]

    [SerializeField]
    private Button optButton;
    [SerializeField]
    private TMP_Dropdown playerMovementDropdown;
    [SerializeField]
    private Button portal1ColorButton;
    [SerializeField]
    private Button portal2ColorButton;
    [SerializeField]
    private TMP_InputField playerNameInput;
    [SerializeField]
    private Toggle showTimerToggle;
    [SerializeField]
    private Toggle portalSplitToggle;
    [SerializeField]
    private Toggle needToTouchGroundToggle;
    [SerializeField]
    private Toggle rotateCameraToggle;

    #endregion

    [SerializeField]
    private TextMeshProUGUI playerNameErrorText;

    public PlatformType platform = PlatformType.Computer;
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
        
        SetSettingsValuesToMatchSaved();

        if (isMobile())
        {
            // If on mobile, set the platform type to Phone
            platform = PlatformType.Phone;
            Debug.Log("Running on mobile device");
        }
        else
        {
            // If on computer, set the platform type to Computer
            platform = PlatformType.Computer;
            Debug.Log("Running on computer");
        }
        // playerNameInput.GetComponent<TMP_InputField>().text = playerLeaderboardName;
        loaded = true;
    }

    #if !UNITY_EDITOR && UNITY_WEBGL

        [System.Runtime.InteropServices.DllImport("__Internal")]
        public static extern bool IsMobileBrowser();
      
        [System.Runtime.InteropServices.DllImport("__Internal")]
        public static extern bool IsPreferredDesktopPlatform();
    #else
            public static bool IsMobileBrowser() => false;
            public static bool IsPreferredDesktopPlatform() => true;
    #endif

    /// <summary>
    /// Returns if the game is WebGL and running on a mobile device
    /// </summary>
    /// <returns>if the game is on WebGL on a mobile device</returns>
    public bool isMobile()
    {
        return IsMobileBrowser();
    }

    #region Saved Settings Getters
    /// <summary>
    /// Makes the settings in the menu match what is 
    /// </summary>
    async private void SetSettingsValuesToMatchSaved()
    {
        GetSavedOpt();
        GetSavedPortalColors();
        playerLeaderboardName = await AuthenticationService.Instance.GetPlayerNameAsync();
        GetSavedTimerVisibility();
        GetSavedPlayerMovementType();
        GetSavedRotateCameraWithGravity();
        GetSavedPortalSplit();
        GetSavedPortalEntering();
    }


    async private void GetSavedOpt()
    {
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
        else 
        {
            // If the key doesn't exist, default to opted in
            optedIn = true;
            AnalyticsService.Instance.StartDataCollection();
        }
    }

    async private void GetSavedTimerVisibility()
    {
        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"ShowTimer"});
        if (playerData.TryGetValue("ShowTimer", out var choice)) 
        {
            Debug.Log($"Timer: {choice.Value.GetAs<bool>()}");
            if (choice.Value.GetAs<bool>())
            {
                showTimer = true;
            }
            else
            {
                showTimer = false;
            }
        }
        else
        {
            // If the key doesn't exist, default to true
            showTimer = true;
        }
    }

    async private void GetSavedPlayerMovementType()
    {
        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"PlayerMovementType"});
        if (playerData.TryGetValue("PlayerMovementType", out var playerMovementType))
        {
            movement = (PlayerMovementType)playerMovementType.Value.GetAs<int>();
            playerMovementDropdown.value = (int)movement;
        }
        else
        {
            // If the key doesn't exist, generate a random choice
            movement = (PlayerMovementType)Random.Range(0, 2);
            var saveMovement = new Dictionary<string, object> { { "PlayerMovementType", (int)movement } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(saveMovement);
            playerMovementDropdown.value = (int)movement;
        }
    }

    async private void GetSavedRotateCameraWithGravity()
    {
        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"RotateCameraWithGravity"});
        if (playerData.TryGetValue("RotateCameraWithGravity", out var choice)) 
        {
            Debug.Log($"RotateCameraWithGravity: {choice.Value.GetAs<bool>()}");
            if (choice.Value.GetAs<bool>())
            {
                rotateCameraWithGravity = true;
            }
            else
            {
                rotateCameraWithGravity = false;
            }
        }
        else
        {
            // If the key doesn't exist, default to true
            rotateCameraWithGravity = true;
        }
    }

    async private void GetSavedPortalSplit()
    {
        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"LeftClickForBothPortals"});
        if (playerData.TryGetValue("LeftClickForBothPortals", out var choice)) 
        {
            Debug.Log($"LeftClickForBothPortals: {choice.Value.GetAs<bool>()}");
            if (choice.Value.GetAs<bool>())
            {
                leftClickForBothPortals = true;
            }
            else
            {
                leftClickForBothPortals = false;
            }
        }
        else
        {
            // If the key doesn't exist, default to true
            leftClickForBothPortals = true;
        }
    }

    async private void GetSavedPortalEntering()
    {
        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"NeedToTouchGroundToReenterPortal"});
        if (playerData.TryGetValue("NeedToTouchGroundToReenterPortal", out var choice)) 
        {
            Debug.Log($"NeedToTouchGroundToReenterPortal: {choice.Value.GetAs<bool>()}");
            if (choice.Value.GetAs<bool>())
            {
                needToTouchGroundToReenterPortal = true;
            }
            else
            {
                needToTouchGroundToReenterPortal = false;
            }
        }
        else
        {
            // If the key doesn't exist, default to true
            needToTouchGroundToReenterPortal = true;
        }
    }

    #endregion
    #region Settings Savers

    async public void SetRotateCameraWithGravity()
    {
        rotateCameraWithGravity = rotateCameraToggle.isOn;
        var result = new Dictionary<string, object> { { "RotateCameraWithGravity", rotateCameraWithGravity } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(result);
    }

    async public void SetPortalsSplit()
    {
        leftClickForBothPortals = portalSplitToggle.isOn;
        var result = new Dictionary<string, object> { { "LeftClickForBothPortals", leftClickForBothPortals } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(result);
    }

    async public void SetPortalEntering()
    {
        needToTouchGroundToReenterPortal = needToTouchGroundToggle.isOn;
        var result = new Dictionary<string, object> { { "NeedToTouchGroundToReenterPortal", needToTouchGroundToReenterPortal } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(result);
    }
    
    async public void SetTimerVisibility()
    {
        showTimer = showTimerToggle.isOn;
        var result = new Dictionary<string, object> { { "ShowTimer", showTimer } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(result);
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
    
    async public void SetPlayerMovement()
    {
        movement = (PlayerMovementType)playerMovementDropdown.value;
        var saveMovement = new Dictionary<string, object> { { "PlayerMovementType", playerMovementDropdown.value } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(saveMovement);
    }

    async public void GetSavedPortalColors()
    {
        var portal1Data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"Portal1Color"});
        if (portal1Data.TryGetValue("Portal1Color", out var portal1ColorValue))
        {
            string portal1ColorHex = "#" + portal1ColorValue.Value.GetAs<string>();
            Debug.Assert(ColorUtility.TryParseHtmlString(portal1ColorHex, out portal1Color),
                "Couldn't parse saved portal 1 color");
        }
        var portal2Data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"Portal2Color"});
        if (portal2Data.TryGetValue("Portal2Color", out var portal2ColorValue))
        {
            string portal2ColorHex = "#" + portal2ColorValue.Value.GetAs<string>();
            Debug.Assert(ColorUtility.TryParseHtmlString(portal2ColorHex, out portal2Color), 
                "Couldn't parse saved portal 2 color");
        }
        SetButtonColor(portal1Color, portal1ColorButton);
        SetButtonColor(portal2Color, portal2ColorButton);
    }

    async public void SavePortalColors()
    {
        string portal1hex = ColorUtility.ToHtmlStringRGB(portal1Color);
        string portal2hex = ColorUtility.ToHtmlStringRGB(portal2Color);
        var savePortal1Color = new Dictionary<string, object> { { "Portal1Color", portal1hex } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(savePortal1Color);
        var savePortal2Color = new Dictionary<string, object> { { "Portal2Color", portal2hex } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(savePortal2Color);
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
            
            // StartCoroutine(SavePortalColor("Portal1Color", portal1Color));
            
            yield return new WaitForSeconds(0.1f);
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

            // StartCoroutine(SavePortalColor("Portal2Color", portal1Color));

            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion

    public void MakeSettingsUIMatchSaved()
    {
        optButton.GetComponentInChildren<TextMeshProUGUI>().text = !optedIn ? "Opt In" : "Opt Out";
        GetSavedPortalColors();
        playerNameInput.GetComponent<TMP_InputField>().text = playerLeaderboardName;
        playerMovementDropdown.value = (int)movement;

        showTimerToggle.isOn = showTimer;
        rotateCameraToggle.isOn = rotateCameraWithGravity;
        portalSplitToggle.isOn = leftClickForBothPortals;
        needToTouchGroundToggle.isOn = needToTouchGroundToReenterPortal;
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

public enum PlayerMovementType
{
    Quick,
    Normal,
}

public enum PlatformType
{
    Phone,
    Computer
}

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
    public bool optedIn = true;
    public PlayerMovementType movement = PlayerMovementType.Normal;

    public Color portal1Color;
    private Color portal1SavedColor;
    [SerializeField]
    private Button portal1ColorButton;
    private bool settingPortal1Color = false;

    public Color portal2Color;
    private Color portal2SavedColor;
    [SerializeField]
    private Button portal2ColorButton;
    private bool settingPortal2Color = false;
    [SerializeField]
    private ColorPickerControl colorPicker;

    public TMP_Dropdown playerMovementDropdown;

    public bool participateInLeaderboard = true;
    public string playerLeaderboardName = "";
    [SerializeField]
    public TMP_InputField playerNameInput;
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
        
        GetOpt();

        GetPlayerMovementType();

        playerLeaderboardName = await AuthenticationService.Instance.GetPlayerNameAsync();
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

    async private void GetOpt()
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

    async private void GetPlayerMovementType()
    {
        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"PlayerMovementType"});
        if (playerData.TryGetValue("PlayerMovementType", out var playerMovementType))
        {
            movement = (PlayerMovementType)playerMovementType.Value.GetAs<int>();
            // playerMovementDropdown.value = (int)movement;
        }
        else
        {
            // If the key doesn't exist, generate a random choice
            movement = (PlayerMovementType)Random.Range(0, 2);
            var saveMovement = new Dictionary<string, object> { { "PlayerMovementType", (int)movement } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(saveMovement);
            // playerMovementDropdown.value = (int)movement;
        }
    }

    /// <summary>
    /// Returns if the game is WebGL and running on a mobile device
    /// </summary>
    /// <returns>if the game is on WebGL on a mobile device</returns>
    public bool isMobile()
    {
        return IsMobileBrowser();
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

    async public void SetPlayerMovement()
    {
        movement = (PlayerMovementType)playerMovementDropdown.value;
        var saveMovement = new Dictionary<string, object> { { "PlayerMovementType", playerMovementDropdown.value } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(saveMovement);
        // playerMovementDropdown.value = (int)movement;
    }

    async public void SetPortalButtonColors()
    {
        var portal1Data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"Portal1Color"});
        if (portal1Data.TryGetValue("Portal1Color", out var portal1ColorValue))
        {
            portal1Color = portal1ColorValue.Value.GetAs<Color>();
        }
        var portal2Data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"Portal2Color"});
        if (portal2Data.TryGetValue("Portal2Color", out var portal2ColorValue))
        {
            portal2Color = portal1ColorValue.Value.GetAs<Color>();
        }
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

    // private IEnumerator SavePortalColor(string key, Color color)
    // {
    //     yield return new WaitForSeconds(3);
    //     if (color != )
    //         yield break; // If saving was cancelled, exit the coroutine
    //     var saveColor = new Dictionary<string, object> { { key, color } };
    //     CloudSaveService.Instance.Data.Player.SaveAsync(saveColor);
    // }

    public void InitializePlayerMovementType()
    {
        GetPlayerMovementType();
        playerMovementDropdown.value = (int)movement;
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

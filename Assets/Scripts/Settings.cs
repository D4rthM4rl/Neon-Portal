using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

public class Settings : MonoBehaviour
{
    public static Settings instance;
    public bool rotateCameraWithGravity = true;
    public bool showTimer = true;

    public bool optedIn = false;
    public bool loaded = false;

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

    // Update is called once per frame
    void Update()
    {
        
    }
}

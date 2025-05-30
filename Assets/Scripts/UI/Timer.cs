using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Analytics;
using TMPro;

public class Timer : MonoBehaviour
{
    public static Timer instance;
    public TextMeshProUGUI timerText;
    public float levelTimer = 0f;
    public float unresetLevelTimer = 0;
    public float inactivityTimer = 0;
    public float sessionTimer = 0;

    private float numMinutesForInactive = 10;
    private bool recordedInactivityEvent = false;

    public Level lastLevelPlayed;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            timerText = GetComponentInChildren<TextMeshProUGUI>();
            inactivityTimer = 0;
            sessionTimer = 0;
        }
        else
            Destroy(gameObject);
    }

    private void Update() 
    {
        inactivityTimer += Time.unscaledDeltaTime;
        unresetLevelTimer += Time.unscaledDeltaTime;
        sessionTimer += Time.unscaledDeltaTime;

        if (inactivityTimer >= numMinutesForInactive * 60 && !recordedInactivityEvent)
        {
            recordedInactivityEvent = true;
            RecordInactivityEvent();
        }
    }

    public void ResetInactivityTimer()
    {
        inactivityTimer = 0;
        recordedInactivityEvent = false;
    }

    public void UpdateTimer()
    {
        if (gameObject.activeSelf == false)
        {
            return;
        }
		levelTimer += Time.deltaTime;
        int minutes = (int)levelTimer / 60;
        int seconds = (int)levelTimer % 60;
        if (seconds < 10)
        {
            timerText.text = "Time: " + minutes + ":0" + seconds;
        }
        else
        {
            timerText.text = "Time: " + minutes + ":" + seconds;
        }
	}

    private void RecordInactivityEvent()
    {
        inactive inactiveEvent;
        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        // If not in a level
        if (levelName[0] != 'W' || !levelName.Contains('L'))
        {
            if (lastLevelPlayed == null) {
                levelName = "None";
                inactiveEvent = new inactive
                {
                    level = levelName,
                    session_time = Mathf.RoundToInt(sessionTimer),
                    movement_type = (int)Settings.instance.movement,
                };
            }
            else 
            {
                levelName = lastLevelPlayed.ToString();
                inactiveEvent = new inactive
                {
                    level = levelName,
                    level_beaten = lastLevelPlayed.beaten,
                    session_time = Mathf.RoundToInt(sessionTimer),
                    movement_type = (int)Settings.instance.movement,
                };
            }
        }
        else // If in a level
        {
            Level level = LevelSelect.instance.GetLevelByName(levelName);
            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
            inactiveEvent = new inactive
            {
                level = levelName,
                level_beaten = level.beaten,
                x_pos = player.transform.position.x,
                y_pos = player.transform.position.y,
                num_deaths = player.numDeaths,
                num_resets = player.numResets,
                unreset_timer = unresetLevelTimer,
                session_time = Mathf.RoundToInt(sessionTimer),
                movement_type = (int)Settings.instance.movement,
            };
            if (PortalGun.portalsInScene.Length > 0 && PortalGun.portalsInScene[0] != null)
            {
                Vector3 portalPos = PortalGun.portalsInScene[0].transform.position;
                inactiveEvent.portal1_x = portalPos.x;
                inactiveEvent.portal1_y = portalPos.y;
            }
            if (PortalGun.portalsInScene.Length > 1 && PortalGun.portalsInScene[1] != null)
            {
                Vector3 portalPos = PortalGun.portalsInScene[1].transform.position;
                inactiveEvent.portal2_x = portalPos.x;
                inactiveEvent.portal2_y = portalPos.y;
            }
            AnalyticsService.Instance.RecordEvent(inactiveEvent);
        }
        
    }
}

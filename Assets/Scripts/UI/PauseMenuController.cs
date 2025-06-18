using UnityEngine;
using System.Collections;
using Unity.Services.Analytics;
using TMPro;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject pauseMenuUI;
    [SerializeField]
    private GameObject mainMenuUI;

    private float originalTimeScale;
    public static PauseMenuController instance;
    public bool isPaused = false;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (pauseMenuUI == null)
                Debug.Log("Pause won't work in this scene");
            else
                DontDestroyOnLoad(pauseMenuUI.transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        

        // Ensure the menu is hidden at startup
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
    }

    public void ToggleMenu()
    {
        Timer.instance.ResetInactivityTimer();
        if (pauseMenuUI.activeSelf)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>Resumes game (sets timeScale back) </summary>
    public void Resume()
    {
        Timer.instance.ResetInactivityTimer();
        Time.timeScale = originalTimeScale;
        if (Settings.instance.showTimer == true)
        {
            Timer.instance.timerText.enabled = true;
        }
        else
        {
            Timer.instance.timerText.enabled = false;
        }
        pauseMenuUI.SetActive(false);
        foreach (GameObject text in GameObject.FindGameObjectsWithTag("Not On Pause"))
        {
            text.gameObject.SetActive(true);
        }
        isPaused = false;
    }

    public void Pause()
    {
        Timer.instance.ResetInactivityTimer();
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Timer.instance.timerText.enabled = false;
        pauseMenuUI.SetActive(true);
        foreach (GameObject text in GameObject.FindGameObjectsWithTag("Not On Pause"))
        {
            text.gameObject.SetActive(false);
        }
        isPaused = true;
    }

    public void Exit()
    {
        Timer.instance.ResetInactivityTimer();
        RecordLevelQuitEvent();
        pauseMenuUI.SetActive(false);
        MainMenu.instance.gameObject.SetActive(true);
        isPaused = false;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
    }

    public void OpenLevelSelect()
    {
        Exit();
        MainMenu.instance.OpenLevelSelect();
    }

    // Call this from the Options button
    public void OpenOptions()
    {
        Timer.instance.ResetInactivityTimer();
        // Implement options menu functionality
        Debug.Log("Options menu requested");
    }

    private void RecordLevelQuitEvent()
    {
        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Level level = LevelSelect.instance.GetLevelByName(levelName);
        Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        level_quit levelQuitEvent = new level_quit
        {
            level = levelName,
            level_beaten = level.beaten,
            x_pos = player.transform.position.x,
            y_pos = player.transform.position.y,
            num_deaths = player.numDeaths,
            num_resets = player.numResets,
            unreset_timer = Timer.instance.unresetLevelTimer,
            session_time = Mathf.RoundToInt(Timer.instance.sessionTimer),
            movement_type = (int)Settings.instance.movement
        };
        if (PortalGun.portalsInScene.Length > 0 && PortalGun.portalsInScene[0] != null)
        {
            Vector3 portalPos = PortalGun.portalsInScene[0].transform.position;
            levelQuitEvent.portal1_x = portalPos.x;
            levelQuitEvent.portal1_y = portalPos.y;
        }
        if (PortalGun.portalsInScene.Length > 1 && PortalGun.portalsInScene[1] != null)
        {
            Vector3 portalPos = PortalGun.portalsInScene[1].transform.position;
            levelQuitEvent.portal2_x = portalPos.x;
            levelQuitEvent.portal2_y = portalPos.y;
        }
        AnalyticsService.Instance.RecordEvent(levelQuitEvent);
    }
}
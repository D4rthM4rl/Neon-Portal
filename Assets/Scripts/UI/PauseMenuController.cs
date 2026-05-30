using UnityEngine;
using System.Collections;
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
            Debug.Log("PauseMenuController instance already exists, destroying duplicate.");
            Destroy(gameObject);
        }
        

        // Ensure the menu is hidden at startup
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
    }

    /// <summary>Toggle (turn off if on/on if off) the pause menu.</summary>
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

    /// <summary>Resumes game (sets timeScale back).</summary>
    public void Resume()
    {
        Timer.instance.ResetInactivityTimer();
        Time.timeScale = originalTimeScale;
        if (Settings.UsesTouchControls) MobileControls.instance.Enable();
        if (Settings.instance.showTimer == true) Timer.instance.Enable();
        else Timer.instance.Disable();
        pauseMenuUI.SetActive(false);
        foreach (GameObject text in GameObject.FindGameObjectsWithTag("Not On Pause"))
        {
            text.gameObject.SetActive(true);
        }
        isPaused = false;
    }

    /// <summary>Pause the game (set timeScale to 0) and open the pause menu.</summary>
    public void Pause()
    {
        Timer.instance.ResetInactivityTimer();
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Timer.instance.Disable();
        pauseMenuUI.SetActive(true);
        foreach (GameObject text in GameObject.FindGameObjectsWithTag("Not On Pause"))
        {
            text.gameObject.SetActive(false);
        }
        MobileControls.instance.Disable();
        isPaused = true;
    }

    /// <summary>Exit the level to the main menu.</summary>
    public void Exit()
    {
        RecordLevelQuitEvent();
        UnityEngine.SceneManagement.SceneManager.LoadScene("After");
        pauseMenuUI.SetActive(false);
        isPaused = false;
        Transition.instance.GoToMainMenu();
    }

    /// <summary>Exit the level and open the level select menu.</summary>
    public void OpenLevelSelect()
    {
        Exit();
        MainMenu.instance.OpenLevelSelect();
    }

    // Call this from the Options button. TODO: make an options button?
    public void OpenOptions()
    {
        Timer.instance.ResetInactivityTimer();
        // Implement options menu functionality
        Debug.Log("Options menu requested");
    }

    /// <summary>
    /// Records an event to Unity analytics that a level was quit.
    /// </summary>
    private void RecordLevelQuitEvent()
    {
        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Level level = LevelSelect.instance.GetLevelByName(levelName);
        Player player = Player.instance;
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
        OnlineServices.RecordEvent(levelQuitEvent);
    }
}
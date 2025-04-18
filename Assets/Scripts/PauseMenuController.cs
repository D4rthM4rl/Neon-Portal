using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject pauseMenuUI;

    private float originalTimeScale;
    public static PauseMenuController instance;
    
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure the menu is hidden at startup
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
    }

    public void ToggleMenu()
    {
        if (pauseMenuUI.activeSelf)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>Resumes game (sets timeScale back, </summary>
    public void Resume()
    {
        Time.timeScale = originalTimeScale;
        pauseMenuUI.SetActive(false);
    }

    public void Pause()
    {
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
    }

    // Call this from the Quit button
    public void QuitGame()
    {
        pauseMenuUI.SetActive(false);
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Call this from the Options button
    public void OpenOptions()
    {
        // Implement options menu functionality
        Debug.Log("Options menu requested");
    }
}
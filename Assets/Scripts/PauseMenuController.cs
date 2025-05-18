using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject pauseMenuUI;

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
        foreach (GameObject text in GameObject.FindGameObjectsWithTag("Not On Pause"))
        {
            text.gameObject.SetActive(true);
        }
        isPaused = false;
    }

    public void Pause()
    {
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
        foreach (GameObject text in GameObject.FindGameObjectsWithTag("Not On Pause"))
        {
            text.gameObject.SetActive(false);
        }
        isPaused = true;
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

    public void GoHome()
    {
        pauseMenuUI.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if  (player != null)
        {
            player.GetComponent<Player>().portalGun.DestroyIndicators();
            Destroy(player);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
    }

    // Call this from the Options button
    public void OpenOptions()
    {
        // Implement options menu functionality
        Debug.Log("Options menu requested");
    }
}
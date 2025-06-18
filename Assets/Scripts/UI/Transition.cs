using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Transition : MonoBehaviour
{
    public Image fadeImage; // Drag the fullscreen black Image here
    public float fadeDuration = 1f;

    public float secBetweenObjectFades = 0.3f;

    public TextMeshProUGUI levelCompleteText; // Text to display the current level
    public TextMeshProUGUI nextLevelText; // Text to show the next level

    [SerializeField]
    private Button nextLevelButton;
    [SerializeField]
    private GameObject inBetweenMenu;


    private Level prevLevel;
    private Level nextLevel;

    public static Transition instance;

    void Start()
    {
        instance = this;
    }

    public void StartTransition(int world, int level, float time)
    {
        inBetweenMenu.SetActive(true);
        Timer.instance.timerText.enabled = false;
        Time.timeScale = 0f;
        StartCoroutine(ChooseNext(world, level, time));
    }

    /// <summary>
    /// Give the option to replay, next level, or return to main menu.
    /// </summary>
    /// <param name="world">World of level that was just completed</param>
    /// <param name="level">Level that was just completed</param>
    private IEnumerator ChooseNext(int world, int level, float time)
    {
        Debug.Log("Transitioning");
        
        Scene currentScene = SceneManager.GetActiveScene();

        StartCoroutine(FadeAsync(0f, 1f)); // Fade out
        yield return new WaitForSecondsRealtime(fadeDuration/2);

        levelCompleteText.text = "World " + world + ", Level " + level + 
            '\n' + "Completed in " + time.ToString("F2") + "s";

        prevLevel = LevelSelect.instance.levels[world - 1, level - 1];
        nextLevel = LevelSelect.instance.GetNextLevel(prevLevel);
        if (nextLevel == null)
        {
            nextLevelButton.gameObject.SetActive(false); // Hide next level button if no next level
        }
    }

    private IEnumerator LoadLevel(Level currLevel)
    {
        // nextLevelText.text = "World " + world + '\n' + "Level " + level; // Update level text
        // nextLevelText.enabled = true; // Show level text
        yield return new WaitForSecondsRealtime(fadeDuration/2); // Wait for fade in to complete

        SceneManager.LoadScene(currLevel.ToString());
        foreach (SpriteRenderer sr in FindObjectsOfType<SpriteRenderer>())
        {
            sr.enabled = false;
        }

        StartCoroutine(FadeAsync(1f, 0f)); // Fade in
        // nextLevelText.enabled = false; // Hide level text after a short delay
    }

    /// <summary>
    /// Fades the screen from one alpha value to another over the specified duration.
    /// </summary>
    /// <param name="from">Alpha value of overlay to start with</param>
    /// <param name="to">Alpha value of overlay to end with</param>
    IEnumerator FadeAsync(float from, float to)
    {
        float timer = 0f;
        Color c = fadeImage.color;

        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(from, to, timer / fadeDuration);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        fadeImage.color = new Color(c.r, c.g, c.b, to);
    }

    private IEnumerator FadeInAllObjectsAsync()
    {
        // Normal plats
        GameObject[] ground = GameObject.FindGameObjectsWithTag("Ground");
        List<GameObject> platforms = new List<GameObject>();
        List<GameObject> movables = new List<GameObject>();
        List<GameObject> normalGround = new List<GameObject>();
        foreach (GameObject obj in ground)
        {
            if (obj.GetComponent<PlatformEffector2D>() != null)
            {
                platforms.Add(obj);
            }
            else if (obj.GetComponent<Teleportable>() != null)
            {
                movables.Add(obj);
            }
            else
            {
                normalGround.Add(obj);
            }
        }
        FadeInObjects(normalGround.ToArray());
        yield return new WaitForSeconds(secBetweenObjectFades);

        // 1-Way platforms
        FadeInObjects(platforms.ToArray());
        yield return new WaitForSeconds(secBetweenObjectFades);

        // Movable blocks
        FadeInObjects(movables.ToArray());
        yield return new WaitForSeconds(secBetweenObjectFades);

        // Unportalable areas
        FadeInObjects(GameObject.FindGameObjectsWithTag("Unportalable"));
        yield return new WaitForSeconds(secBetweenObjectFades);

        // Gravity zones
        yield return new WaitForSeconds(secBetweenObjectFades);

        // Indicators
        yield return new WaitForSeconds(secBetweenObjectFades);

        // Player and exit
        yield return new WaitForSeconds(secBetweenObjectFades);

        // Background
        GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
        if (camera != null)
        {
            SpriteRenderer bg = camera.GetComponentInChildren<SpriteRenderer>();
            if (bg != null)
            {
                bg.enabled = true; // Enable background
            }
        }
    }

    private void FadeInObjects(GameObject[] objectsToFade)
    {
        foreach (GameObject obj in objectsToFade)
        {
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
            }
        }
    }

    public void NextLevel()
    {
        Debug.Assert(nextLevel != null, "Next level is null. Cannot load next level.");
        inBetweenMenu.SetActive(false);
        Timer.instance.timerText.enabled = true;
        StartCoroutine(LoadLevel(nextLevel));
    }

    /// <summary>
    /// Reloads the previous level when the retry button is pressed
    /// </summary>
    public void RetryLevel()
    {
        inBetweenMenu.SetActive(false);
        Timer.instance.ResetInactivityTimer();
        Timer.instance.timerText.enabled = true;
        StartCoroutine(LoadLevel(prevLevel));
    }

    public void GoToLevelSelect()
    {
        GoToMainMenu();
        MainMenu.instance.OpenLevelSelect();
    }

    public void GoToMainMenu()
    {
        inBetweenMenu.SetActive(false);
        Time.timeScale = 1f;
        MainMenu.instance.gameObject.SetActive(true);

        Timer.instance.ResetInactivityTimer();
        // RecordLevelQuitEvent();
        MainMenu.instance.gameObject.SetActive(true);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
    }
}
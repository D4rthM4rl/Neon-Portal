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
        StartCoroutine(FadeAsync(0f, 1f)); // Fade out
        yield return new WaitForSecondsRealtime(fadeDuration); // Wait for fade out to complete
        inBetweenMenu.SetActive(true);
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

        // SceneManager.LoadScene(currLevel.ToString());
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(currLevel.ToString());
        while (!loadOp.isDone)
            yield return null;
        StartCoroutine(FadeAllObjectsAsync(0, true));

        StartCoroutine(FadeAsync(1f, 0f, 0)); // Fade in
        StartCoroutine(FadeAllObjectsAsync(.3f, false)); // Fade in all objects
        // nextLevelText.enabled = false; // Hide level text after a short delay
    }

    /// <summary>
    /// Fades the screen from one alpha value to another over the specified duration.
    /// </summary>
    /// <param name="from">Alpha value of overlay to start with</param>
    /// <param name="to">Alpha value of overlay to end with</param>
    IEnumerator FadeAsync(float from, float to, float duration = 1f)
    {
        float timer = 0f;
        Color c = fadeImage.color;

        while (timer < duration)
        {
            float alpha = Mathf.Lerp(from, to, timer / duration);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        fadeImage.color = new Color(c.r, c.g, c.b, to);
    }

    private IEnumerator FadeAllObjectsAsync(float secBetweenFades = 0.3f, bool fadeOut = false)
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
        if (FadeObjects(normalGround.ToArray(), fadeOut))
            yield return new WaitForSecondsRealtime(secBetweenFades);

        // 1-Way platforms
        if (FadeObjects(platforms.ToArray(), fadeOut))
            yield return new WaitForSecondsRealtime(secBetweenFades);

        // Movable blocks
        if (FadeObjects(movables.ToArray(), fadeOut))
            yield return new WaitForSecondsRealtime(secBetweenFades);

        // Unportalable areas
        if (FadeObjects(GameObject.FindGameObjectsWithTag("Unportalable"), fadeOut))
            yield return new WaitForSecondsRealtime(secBetweenFades);

        // Gravity zones
        if (FadeObjects(GameObject.FindGameObjectsWithTag("Gravity Zone"), fadeOut))
            yield return new WaitForSecondsRealtime(secBetweenFades);

        // Indicators
        if (FadeObjects(GameObject.FindGameObjectsWithTag("Indicator"), fadeOut))
            yield return new WaitForSecondsRealtime(secBetweenFades);

        // Player and exit
        FadeObjects(GameObject.FindGameObjectsWithTag("Level Exit"), fadeOut);
        FadeObjects(GameObject.FindGameObjectsWithTag("Player"), fadeOut);
        yield return new WaitForSecondsRealtime(secBetweenFades);

        // Background
        GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
        if (camera != null)
        {
            SpriteRenderer bg = camera.GetComponentInChildren<SpriteRenderer>();
            if (bg != null)
            {
                bg.enabled = !fadeOut;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectsToFade"></param>
    /// <returns>True if there was anything to fade</returns>
    private bool FadeObjects(GameObject[] objectsToFade, bool fadeOut = true)
    {
        if (objectsToFade.Length == 0)
        {
            return false; // Nothing to fade
        }
        bool anySprites = false;
        foreach (GameObject obj in objectsToFade)
        {
            SpriteRenderer[] sr = obj.GetComponentsInChildren<SpriteRenderer>();
            if (sr != null)
            {
                foreach (SpriteRenderer spriteRenderer in sr)
                {
                    spriteRenderer.enabled = true; // Enable sprite renderers
                    anySprites = true; // At least one sprite was found
                }
            }
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                img.enabled = true;
                anySprites = true;
            }
        }
        return anySprites;
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
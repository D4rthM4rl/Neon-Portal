using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Transition : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;

    public float secBetweenObjectFades = 0.3f;

    /// <summary>Text to display the current level.</summary>
    [SerializeField] private TextMeshProUGUI levelCompleteText; 
    /// <summary>Text to show the next level option.</summary>
    [SerializeField] private TextMeshProUGUI nextLevelText;

    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI bestTimeText;


    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button starsButton;
    [SerializeField] private Button myRanksButton;
    [SerializeField] private Button top20Button;

    [SerializeField] private GameObject inBetweenMenu;


    private Level prevLevel;
    private Level nextLevel;

    public static Transition instance;

    void Awake()
    {
        instance = this;
    }

    public void StartTransition(int world, int level, float time, float prevBest)
    {
        Timer.instance.Enable();
        Time.timeScale = 0f;
        MobileControls.instance.Disable();
        StartCoroutine(ChooseNext(world, level, time, prevBest));
    }

    /// <summary>Give the option to replay, next level, or return to main menu.</summary>
    /// <param name="world">World of level that was just completed.</param>
    /// <param name="level">Level that was just completed.</param>
    /// <param name="time">Time in seconds the level was just beaten in.</param>
    /// <param name="prevBest">Best time in seconds the level was beaten in before this.</param>
    private IEnumerator ChooseNext(int world, int level, float time, float prevBest)
    {
        StartCoroutine(FadeAsync(0f, 1f)); // Fade out
        yield return new WaitForSecondsRealtime(fadeDuration); // Wait for fade out to complete
        inBetweenMenu.SetActive(true);
        
        levelCompleteText.text = "World " + world + ", Level " + level;

        prevLevel = LevelSelect.instance.levels[world - 1, level - 1];
        nextLevel = LevelSelect.instance.GetNextLevel(prevLevel);

        Leaderboard.instance.ShowTransitionStars(prevLevel, time, prevBest);
        myRanksButton.onClick.RemoveAllListeners();
        myRanksButton.onClick.AddListener(() => Leaderboard.instance.ShowTransitionLeaderboardMyRanks(prevLevel));
        starsButton.onClick.RemoveAllListeners();
        starsButton.onClick.AddListener(() => Leaderboard.instance.ShowTransitionStars(prevLevel, time, prevBest));
        top20Button.onClick.RemoveAllListeners();
        top20Button.onClick.AddListener(() => Leaderboard.instance.ShowTransitionLeaderboardTop20(prevLevel));
        Leaderboard.instance.ShowTransitionStars(prevLevel, time, prevBest);


        if (nextLevel == null)
        {
            nextLevelButton.gameObject.SetActive(false); // Hide next level button if no next level
        }
    }

    public void LoadLevelFromLevelSelect(Level level)
    {
        StartCoroutine(LoadLevelCoroutine(level));
    }

    public IEnumerator LoadLevelCoroutine(Level level)
    {
        // nextLevelText.text = "World " + world + '\n' + "Level " + level; // Update level text
        // nextLevelText.enabled = true; // Show level text
        // yield return new WaitForSecondsRealtime(fadeDuration/2); // Wait for fade in to complete

        // SceneManager.LoadScene(currLevel.ToString());
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(level.ToString());
        while (!loadOp.isDone)
            yield return null;
        StartCoroutine(FadeAllObjectsAsync(0, true)); // Unload all sprites

        StartCoroutine(FadeAsync(1f, 0f, 0)); // Fade in
        Timer.instance.Enable();
        if (Settings.UsesTouchControls) MobileControls.instance.Enable();
        StartCoroutine(FadeAllObjectsAsync(0.2f, false)); // Fade in all objects
        // nextLevelText.enabled = false; // Hide level text after a short delay
    }

    /// <summary>
    /// Fades the screen from one alpha value to another over the specified duration.
    /// </summary>
    /// <param name="from">Alpha value of overlay to start with</param>
    /// <param name="to">Alpha value of overlay to end with</param>
    public IEnumerator FadeAsync(float from, float to, float duration = 1f)
    {
        float timer = 0f;
        Color c = fadeImage.color;
        fadeImage.gameObject.SetActive(true);

        while (timer < duration)
        {
            float alpha = Mathf.Lerp(from, to, timer / duration);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        fadeImage.color = new Color(c.r, c.g, c.b, to);
        if (to == 0) fadeImage.gameObject.SetActive(false);
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
            else if (obj.GetComponent<Collider2D>())
            {
                normalGround.Add(obj);
            }
        }
        if (FadeObjects(normalGround.ToArray(), fadeOut) && secBetweenFades > 0)
        {
            yield return new WaitForSecondsRealtime(secBetweenFades);
        }

        // 1-Way platforms
        if (FadeObjects(platforms.ToArray(), fadeOut) && secBetweenFades > 0)
        {
            yield return new WaitForSecondsRealtime(secBetweenFades);
        }

        // Movable blocks
        if (FadeObjects(movables.ToArray(), fadeOut) && secBetweenFades > 0)
        {
            yield return new WaitForSecondsRealtime(secBetweenFades);
        }

        // Unportalable areas
        if (FadeObjects(GameObject.FindGameObjectsWithTag("Unportalable"), fadeOut) && secBetweenFades > 0)
        {
            yield return new WaitForSecondsRealtime(secBetweenFades);
        }

        // Gravity zones
        if (FadeObjects(GameObject.FindGameObjectsWithTag("Gravity Zone"), fadeOut) && secBetweenFades > 0)
            yield return new WaitForSecondsRealtime(secBetweenFades);

        // Indicators

        // Player and exit
        FadeObjects(GameObject.FindGameObjectsWithTag("Level Exit"), fadeOut);
        FadeObjects(GameObject.FindGameObjectsWithTag("Player"), fadeOut);
        if (secBetweenFades > 0) yield return new WaitForSecondsRealtime(secBetweenFades);

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
        if (secBetweenFades > 0) yield return new WaitForSecondsRealtime(secBetweenFades);

        FadeObjects(GameObject.FindGameObjectsWithTag("Indicator"), fadeOut);
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
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = !fadeOut; // Enable sprite renderers
                anySprites = true; // At least one sprite was found
            }
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                img.enabled = !fadeOut;
                anySprites = true;
            }
        }
        return anySprites;
    }

    public void NextLevel()
    {
        Debug.Assert(nextLevel != null, "Next level is null. Cannot load next level.");
        inBetweenMenu.SetActive(false);
        StartCoroutine(LoadLevelCoroutine(nextLevel));
    }

    /// <summary>
    /// Reloads the previous level when the retry button is pressed
    /// </summary>
    public void RetryLevel()
    {
        inBetweenMenu.SetActive(false);
        Timer.instance.ResetInactivityTimer();
        StartCoroutine(LoadLevelCoroutine(prevLevel));
    }

    public void GoToLevelSelect()
    {
        GoToMainMenu();
        MainMenu.instance.OpenLevelSelect();
    }

    public void GoToMainMenu()
    {
        StartCoroutine(FadeAsync(0, 1, fadeDuration/2));
        if (Timer.instance) Timer.instance.Disable();
        inBetweenMenu.SetActive(false);
        Time.timeScale = 1f;
        MainMenu.instance.gameObject.SetActive(true);
        // Load an empty scene to clear out the level and because the home scene
        // creates objects that we don't need to remake.
        UnityEngine.SceneManagement.SceneManager.LoadScene("After");
        MainMenu.instance.OpenMainMenu();
        StartCoroutine(FadeAsync(1, 0, fadeDuration/2));

        Timer.instance.ResetInactivityTimer();
    }
}
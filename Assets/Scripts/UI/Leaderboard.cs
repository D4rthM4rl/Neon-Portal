using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;

/// <summary>
/// Entry in the leaderboard containing their display name and completion time for a level
/// </summary>
public record LeaderboardEntry
{
    public int Rank { get; set; }
    public string DisplayName { get; set; }
    public float Time { get; set; }
}

public class Leaderboard : MonoBehaviour
{
    public static Leaderboard instance;

    [SerializeField] private Color evenRowColor = new Color(0.95f, 0.95f, 0.95f);
    [SerializeField] private Color oddRowColor = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] private Color playerHighlightColor = Color.yellow;

    [SerializeField] private Color firstPlaceColor = Color.yellow;
    [SerializeField] private Color secondPlaceColor = Color.gray;
    [SerializeField] private Color thirdPlaceColor = new Color(0.8f, 0.5f, 0f); // bronze
    [SerializeField] private Color defaultRankColor = Color.white;


    [SerializeField] private LeaderboardUI levelSelectLeaderboard;
    [SerializeField] private LeaderboardUI transitionLeaderboard;
    

    void Awake()
    {
        instance = this;
    }

    /// <summary>Makes the leaderboard show up on the level select menu.</summary>
    /// <param name="level">What level to show the leaderboard for.</param>
    public void ShowLevelSelectTop20(Level level) 
    { 
        levelSelectLeaderboard.title.text = "Top 20 Leaderboard";
        levelSelectLeaderboard.rankingsContainer.SetActive(true);
        levelSelectLeaderboard.myRanksButton.interactable = true;
        levelSelectLeaderboard.top20Button.interactable = false;
        levelSelectLeaderboard.starsButton.interactable = true;
        levelSelectLeaderboard.myRanksButton.transform.parent.GetComponent<Image>().enabled = false;
        levelSelectLeaderboard.top20Button.transform.parent.GetComponent<Image>().enabled = true;
        levelSelectLeaderboard.starsButton.transform.parent.GetComponent<Image>().enabled = false;
        levelSelectLeaderboard.refresh.onClick.RemoveAllListeners();
        levelSelectLeaderboard.refresh.onClick.AddListener(() => LoadAndShowTop20(level, levelSelectLeaderboard));
        LoadAndShowTop20(level, levelSelectLeaderboard);
    }

    /// <summary>Makes the leaderboard show up on the level select menu.</summary>
    /// <param name="level">What level to show the leaderboard for.</param>
    public void ShowLevelSelectMyRanks(Level level)
    { 
        levelSelectLeaderboard.rankingsContainer.SetActive(true);
        levelSelectLeaderboard.title.text = "My Rank in Leaderboard";
        levelSelectLeaderboard.myRanksButton.interactable = false;
        levelSelectLeaderboard.top20Button.interactable = true;
        levelSelectLeaderboard.starsButton.interactable = true;
        levelSelectLeaderboard.myRanksButton.transform.parent.GetComponent<Image>().enabled = true;
        levelSelectLeaderboard.top20Button.transform.parent.GetComponent<Image>().enabled = false;
        levelSelectLeaderboard.starsButton.transform.parent.GetComponent<Image>().enabled = false;
        levelSelectLeaderboard.refresh.onClick.RemoveAllListeners();
        levelSelectLeaderboard.refresh.onClick.AddListener(() => LoadAndShowMyRanks(level, levelSelectLeaderboard));
        LoadAndShowMyRanks(level, levelSelectLeaderboard);
    }

    /// <summary>Makes the leaderboard show up on the transition menu.</summary>
    /// <param name="level">What level to show the leaderboard for.</param>
    public void ShowTransitionLeaderboardTop20(Level level) 
    { 
        transitionLeaderboard.title.text = "Top 20 Leaderboard";
        transitionLeaderboard.rankingsContainer.SetActive(true);
        transitionLeaderboard.myRanksButton.interactable = true;
        transitionLeaderboard.top20Button.interactable = false;
        transitionLeaderboard.starsButton.interactable = true;
        transitionLeaderboard.myRanksButton.transform.parent.GetComponent<Image>().enabled = false;
        transitionLeaderboard.top20Button.transform.parent.GetComponent<Image>().enabled = true;
        transitionLeaderboard.starsButton.transform.parent.GetComponent<Image>().enabled = false;
        transitionLeaderboard.refresh.onClick.RemoveAllListeners();
        transitionLeaderboard.refresh.onClick.AddListener(() => LoadAndShowTop20(level, transitionLeaderboard));
        LoadAndShowTop20(level, transitionLeaderboard);
    }

    /// <summary>Makes the leaderboard show up on the transition menu.</summary>
    /// <param name="level">What level to show the leaderboard for.</param>
    public void ShowTransitionLeaderboardMyRanks(Level level) 
    {
        transitionLeaderboard.title.text = "My Rank in Leaderboard";
        transitionLeaderboard.rankingsContainer.SetActive(true);
        transitionLeaderboard.myRanksButton.interactable = false;
        transitionLeaderboard.top20Button.interactable = true;
        transitionLeaderboard.starsButton.interactable = true;
        transitionLeaderboard.myRanksButton.transform.parent.GetComponent<Image>().enabled = true;
        transitionLeaderboard.top20Button.transform.parent.GetComponent<Image>().enabled = false;
        transitionLeaderboard.starsButton.transform.parent.GetComponent<Image>().enabled = false;
        transitionLeaderboard.refresh.onClick.RemoveAllListeners();
        transitionLeaderboard.refresh.onClick.AddListener(() => LoadAndShowMyRanks(level, transitionLeaderboard));
        LoadAndShowMyRanks(level, transitionLeaderboard);
    }

    /// <summary>Shows the stars menu on the transition leaderboard.</summary>
    /// <param name="level">Level that was just beaten.</param>
    /// <param name="time">Time the level was just completed in.</param>
    /// <param name="prevBest">Best time on this level before this run.</param>
    public void ShowTransitionStars(Level level, float time, float prevBest)
    {
        LeaderboardUI ui = transitionLeaderboard;
        transitionLeaderboard.myRanksButton.interactable = true;
        transitionLeaderboard.top20Button.interactable = true;
        transitionLeaderboard.starsButton.interactable = false;
        transitionLeaderboard.myRanksButton.transform.parent.GetComponent<Image>().enabled = false;
        transitionLeaderboard.top20Button.transform.parent.GetComponent<Image>().enabled = false;
        transitionLeaderboard.starsButton.transform.parent.GetComponent<Image>().enabled = true;
        if (time < prevBest)
        {
            ui.title.text = "New Personal Best!";
            ui.title.color = Color.magenta;
            string prevBestString;
            if (prevBest == float.PositiveInfinity) prevBestString = "Unbeaten";
            else prevBestString = prevBest.ToString("F4") + "s";
            ui.bestTimeText.text = $"Previous Best: {prevBestString}";
        }
        else
        {
            ui.title.text = "Personal Best";
            ui.title.color = Color.black;
            string prevBestString;
            if (prevBest == float.PositiveInfinity) prevBestString = "Unbeaten";
            else prevBestString = prevBest.ToString("F4") + "s";
            ui.bestTimeText.text = $"Best: {prevBestString}";
        }
        ui.timeText.text = $"{time.ToString("F4")}s";

        ShowStars(level, time, ui, StarTiers.GetStarTier(level, prevBest));
    }

    /// <summary>Shows the stars menu on the transition leaderboard.</summary>
    /// <param name="level">Level that was just beaten.</param>
    /// <param name="best">Best time on this level.</param>
    public void ShowLevelSelectStars(Level level, float best)
    {
        levelSelectLeaderboard.myRanksButton.interactable = true;
        levelSelectLeaderboard.top20Button.interactable = true;
        levelSelectLeaderboard.starsButton.interactable = false;
        levelSelectLeaderboard.myRanksButton.transform.parent.GetComponent<Image>().enabled = false;
        levelSelectLeaderboard.top20Button.transform.parent.GetComponent<Image>().enabled = false;
        levelSelectLeaderboard.starsButton.transform.parent.GetComponent<Image>().enabled = true;
        string bestString;
        if (best == float.PositiveInfinity) bestString = "Unbeaten";
        else bestString = best.ToString("F4") + "s";
        levelSelectLeaderboard.timeText.text = $"Best: {bestString}";
        levelSelectLeaderboard.title.text = "Personal Best";
        levelSelectLeaderboard.title.color = Color.black;
        ShowStars(level, best, levelSelectLeaderboard);
    }

    /// <summary>Shows any type of leaderboard.</summary>
    /// <param name="level">Level to show the leaderboard for.</param>
    /// <param name="ui">Which leaderboard to show.</param>
    /// <param name="entries">Which entries to show on the leaderboard.</param>
    public void ShowLeaderboard(Level level, LeaderboardUI ui, List<LeaderboardEntry> entries)
    {
        ui.container.SetActive(true);
        ui.scrollView.SetActive(true);
        ui.starUI.container.SetActive(false);

        if (!OnlineServices.online)
        {
            ui.title.text = "Leaderboard" + Environment.NewLine + Environment.NewLine + "Offline";
            ui.title.transform.localPosition = Vector3.up * 32;
            return;
        }

        if (entries == null)
        {
            ui.title.text = "Leaderboard" + Environment.NewLine + Environment.NewLine + "No Entries";
            ui.title.transform.localPosition = Vector3.up * 32;
            return;
        }
        else 
        {
            ui.title.transform.localPosition = Vector3.up * 50;
        }
        
        EnterLeaderboardData(entries, ui);
    }

    private async void LoadAndShowTop20(Level level, LeaderboardUI ui)
    {
        PrepareLeaderboardView(ui);
        ui.title.text = "Top 20 Leaderboard";
        ui.title.transform.localPosition = Vector3.up * 50;

        if (!Settings.instance.participateInLeaderboard)
        {
            ShowLeaderboard(level, ui, null);
            return;
        }

        ui.title.text = "Top 20 Leaderboard" + Environment.NewLine + Environment.NewLine + "Loading...";
        level.top20 = await GetTopPlayers(level, 20);
        ShowLeaderboard(level, ui, level.top20);
    }

    private async void LoadAndShowMyRanks(Level level, LeaderboardUI ui)
    {
        PrepareLeaderboardView(ui);
        ui.title.text = "My Rank in Leaderboard";
        ui.title.transform.localPosition = Vector3.up * 50;

        if (!Settings.instance.participateInLeaderboard)
        {
            ShowLeaderboard(level, ui, null);
            return;
        }

        ui.title.text = "My Rank in Leaderboard" + Environment.NewLine + Environment.NewLine + "Loading...";
        level.myRanks = await GetMyRanks(level);
        ShowLeaderboard(level, ui, level.myRanks);
    }

    private void PrepareLeaderboardView(LeaderboardUI ui)
    {
        ui.container.SetActive(true);
        ui.scrollView.SetActive(true);
        ui.starUI.container.SetActive(false);
        ui.rankingsContainer.SetActive(true);
    }

    /// <summary>
    /// Puts the correct data into the given leaderboard.
    /// </summary>
    /// <param name="entries">Which entries to put in.</param>
    /// <param name="ui">Which leaderboard ot use.</param>
    private void EnterLeaderboardData(List<LeaderboardEntry> entries, LeaderboardUI ui)
    {
        for (int i = 0; i < 20; i++) // Leaderboard will only display 20 at once
        {
            LeaderboardEntry entry = i < entries.Count ? entries[i] : null;
            if (entry == null && i >= ui.leaderboardRows.Count) break; // No more entries and no more rows
            if (entry == null && i < ui.leaderboardRows.Count) // No more entries but still have rows
            {
                ui.leaderboardRows[i].gameObject.SetActive(false);
                continue;
            }
            Color c = Color.black;
            if (entry.DisplayName == Settings.instance.playerLeaderboardName)
            {
                c = Color.magenta;
            }
            if (i >= ui.leaderboardRows.Count) // More entries than rows, need to make more rows
            {
                LeaderboardRow row = Instantiate(ui.rowExample,
                                                 ui.rowParent.transform
                                                ).GetComponent<LeaderboardRow>();
                row.Change(entry, c);
                ui.leaderboardRows.Add(row);
            }
            else // Just change the existing row
            {
                ui.leaderboardRows[i].Change(entry, c);
                ui.leaderboardRows[i].gameObject.SetActive(true);
            }
        }
        
    }

    /// <summary>Shows the stars menu on the given leaderboard.</summary>
    /// <param name="level">Which level to show the stars for.</param>
    /// <param name="bestTime">Best time on this level.</param>
    /// <param name="ui">Which leaderboard UI to use.</param>
    /// <param name="prevStars">If transition, how many stars were gotten before this attempt.</param>
    private void ShowStars(Level level, float bestTime, LeaderboardUI ui, int prevStars = 0)
    {
        ui.refresh.onClick.RemoveAllListeners();
        ui.refresh.onClick.AddListener(() => ShowStars(level, bestTime, ui, prevStars));
        ui.starUI.container.SetActive(true);
        ui.rankingsContainer.SetActive(false);
        ui.scrollView.SetActive(false);
        
        int newStars = StarTiers.GetStarTier(level, bestTime);
        StarUI starUI = ui.starUI;
        starUI.star1Time.text = level.stars.purpleTime.ToString("F2") + "s";
        starUI.star2Time.text = level.stars.blueTime.ToString("F2") + "s";
        starUI.star3Time.text = level.stars.greenTime.ToString("F2") + "s";
        int numStars = Mathf.Max(newStars, prevStars);

        starUI.ColorUI(numStars, numStars <= prevStars);
    }

    /// <summary>
    /// Submits a completion time to the leaderboard.
    /// </summary>
    /// <param name="level">What level to submit the time for.</param>
    /// <param name="time">Time the level was beaten in.</param>
    public async void SubmitTime(Level level, float time)
    {   
        if (Settings.instance.participateInLeaderboard && OnlineServices.online)
        {
            try 
            {
                OnlineServices.AddPlayerScore(level.ToString(), time);
                await GetMyRanks(level);
            }
            catch (Exception e) 
            {
                Debug.LogWarning($"Failed to submit score: {e}");
                throw;
            }
        }
    }

    /// <summary>Gets the top Leaderboard entry (fastest time) for a level.</summary>
    /// <param name="level">What level to get the top score for.</param>
    /// <returns>Leaderboard entry containing </returns>
    public async Task<LeaderboardEntry> GetWorldRecord(Level level)
    {
        if (!Settings.instance.participateInLeaderboard || !OnlineServices.online)
        {
            // Debug.LogWarning("Leaderboard is not enabled or not online.");
            return new LeaderboardEntry { DisplayName = "Not Online", Time = float.PositiveInfinity };
        }
        string levelTitle = "W" + level.world + "L" + level.level;
        try
        {
            string response = await OnlineServices.GetScoresByTier(levelTitle, "Purple");
            if (!OnlineServices.online) return new LeaderboardEntry {
                                                DisplayName = "Offline",
                                                Time = float.PositiveInfinity
                                                };
            string name = "";
            string score = "";
            if (response == "[]")
            {
                return new LeaderboardEntry { DisplayName = "No scores", Time = float.PositiveInfinity };
            }
            else
            {
                name = response.Split(',')[1].Split(':')[1].Replace("\"", "");
                score = response.Split(',')[3].Split(':')[1];
                return new LeaderboardEntry { DisplayName = "No scores", Time = float.Parse(score) };;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to retrieve leaderboard: " + e.Message);
            return new LeaderboardEntry { DisplayName = "No scores", Time = float.PositiveInfinity };
        }
    }

    /// <summary>
    /// Gets the top leaderboard entries for a level.
    /// </summary>
    /// <param name="level">Level to get the entries for.</param>
    /// <param name="howMany">How many of the top entries to get.</param>
    /// <returns>List of the top (fastest) n Leaderboard entries.</returns>
    public async Task<List<LeaderboardEntry>> GetTopPlayers(Level level, int howMany = 10)
    {
        if (!Settings.instance.participateInLeaderboard)
        {
            Debug.LogWarning("Leaderboard is not enabled.");
            return null;
        }
        Debug.Assert(level != null, "Level cannot be null");
        Debug.Assert(howMany > 0, "howMany must be greater than 0");

        string levelTitle = "W" + level.world + "L" + level.level;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(500 * attempt);

            await OnlineServices.WaitForInitializationAsync();
            if (!OnlineServices.online)
            {
                Debug.LogWarning("Leaderboard is not online.");
                return null;
            }

            try
            {
                int offset = 0;
                string response = await OnlineServices.GetScores(levelTitle,
                    new Unity.Services.Leaderboards.GetScoresOptions
                    {
                        Offset = offset, Limit = howMany
                    });
                if (response == "[]" || response == null)
                {
                    Debug.Log("No scores found for this level.");
                    return null;
                }

                List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();
                string name = "";
                string score = "";
                string[] entries = response.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                int rank = offset + 1;
                for (int i = 0; i < entries.Length; i++)
                {
                    string entry = entries[i];
                    if (!entry.Contains(',') || !entry.Contains(':'))
                        continue;
                    name = entry.Split(',')[1].Split(':')[1].Replace("\"", "");
                    score = entry.Split(',')[3].Split(':')[1];
                    leaderboardEntries.Add(new LeaderboardEntry { 
                        DisplayName = name,
                        Time = float.Parse(score),
                        Rank = rank
                    });
                    rank++;
                }
                return leaderboardEntries;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to retrieve leaderboard (attempt " + (attempt + 1) + "): " + e.Message);
            }
        }

        Debug.LogError("Failed to retrieve leaderboard after retries.");
        return null;
    }

    /// <summary>Gets 5 entries on each side of the player.</summary>
    /// <param name="level">Which level to get entries for.</param>
    /// <returns>List of entries around and including the player.</returns>
    public async Task<List<LeaderboardEntry>> GetMyRanks(Level level)
    {
        if (!Settings.instance.participateInLeaderboard)
        {
            Debug.LogWarning("Leaderboard is not enabled.");
            return null;
        }
        Debug.Assert(level != null, "Level cannot be null");
        string levelTitle = "W" + level.world + "L" + level.level;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(500 * attempt);

            await OnlineServices.WaitForInitializationAsync();
            if (!OnlineServices.online)
            {
                Debug.LogWarning("Leaderboard is not online.");
                return null;
            }

            try
            {
                string response = await OnlineServices.GetPlayerRangeAsync(levelTitle);
                if (response == "[]" || response == null)
                {
                    Debug.Log("No scores found for this level.");
                    return null;
                }

                List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();
                string name = "";
                string score = "";
                string[] entries = response.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                string rank = "";
                for (int i = 0; i < entries.Length; i++)
                {
                    string entry = entries[i];
                    if (!entry.Contains(',') || !entry.Contains(':'))
                        continue;
                    name = entry.Split(',')[1].Split(':')[1].Replace("\"", "");
                    rank = entry.Split(',')[2].Split(':')[1].Replace("\"", "");
                    score = entry.Split(',')[3].Split(':')[1];
                    leaderboardEntries.Add(new LeaderboardEntry { 
                        DisplayName = name,
                        Time = float.Parse(score),
                        Rank = (int.Parse(rank) + 1)
                    });
                }
                return leaderboardEntries;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to retrieve my ranks (attempt " + (attempt + 1) + "): " + e.Message);
            }
        }

        Debug.LogError("Failed to retrieve my ranks after retries.");
        return null;
    }
}

[System.Serializable]
public class LeaderboardUI
{
    public TextMeshProUGUI title;
    public GameObject container;
    public GameObject rowExample;
    public GameObject rowParent;
    public Button myRanksButton;
    public Button top20Button;
    public Button starsButton;
    public StarUI starUI;
    public Button refresh;
    public GameObject scrollView;

    /// <summary>Only used on transition leaderboard for most recent time.</summary>
    public TextMeshProUGUI timeText;
    /// <summary>Best time on this level (before current completion if transition leaderboard).</summary>
    public TextMeshProUGUI bestTimeText;
    
    /// <summary>Non-star menu on the leaderboard.</summary>
    public GameObject rankingsContainer;

    [HideInInspector] public List<LeaderboardRow> leaderboardRows = new List<LeaderboardRow>();
}

/// <summary>UI Objects for the star menu</summary>
[System.Serializable] public class StarUI
{
    public GameObject container;

    public GameObject star1BG;
    public GameObject star2BG;
    public GameObject star3BG;

    public TextMeshProUGUI star1Time;
    public TextMeshProUGUI star2Time;
    public TextMeshProUGUI star3Time;

    public Image star1Icon;
    public List<Image> star2Icons;
    public List<Image> star3Icons;

    public Color newStarBG;
    public Color oldStarBG;
    public Color oldStarColor;
    public Color newStar1Color;
    public Color newStar2Color;
    public Color newStar3Color;

    /// <summary>Colors this UI with the correct colors.</summary>
    /// <param name="num">How many stars have been obtained.</param>
    /// <param name="old">Whether the stars are newly or previously obtained/</param>
    public void ColorUI(int num, bool old)
    {
        Color star1Color;
        Color star2Color;
        Color star3Color;
        Color bgColor;
        if (old)
        {
            star1Color = oldStarColor;
            star2Color = oldStarColor;
            star3Color = oldStarColor;
            bgColor = oldStarBG;
        }
        else
        {
            star1Color = newStar1Color;
            star2Color = newStar2Color;
            star3Color = newStar3Color;
            bgColor = newStarBG;
        }

        if (num == 3)
        {
            star3BG.GetComponent<Image>().color = bgColor;
            foreach (Image i in star3Icons) i.color = star3Color;
        }
        else
        {
            star3BG.GetComponent<Image>().color = Color.white;
            foreach (Image i in star3Icons) i.color = newStar3Color;
        }

        if (num >= 2)
        {
            star2BG.GetComponent<Image>().color = bgColor;
            foreach (Image i in star2Icons) i.color = star2Color;
        }
        else
        {
            star2BG.GetComponent<Image>().color = Color.white;
            foreach (Image i in star2Icons) i.color = newStar2Color;
        }

        if (num >= 1)
        {
            star1BG.GetComponent<Image>().color = bgColor;
            star1Icon.color = star1Color;
        }
        else
        {
            star1BG.GetComponent<Image>().color = Color.white;
            star1Icon.color = newStar1Color;
        }
    }
}
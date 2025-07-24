using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.Leaderboards;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;

public record LeaderboardEntry
{
    public string DisplayName { get; set; }
    public float Time { get; set; }
}

public class Leaderboard : MonoBehaviour
{
    public static Leaderboard instance;

    [SerializeField]
    private GameObject lsLeaderboardUI;
    [SerializeField]
    private TextMeshProUGUI lsLeaderboardTitle;
    [SerializeField]
    private GameObject tLeaderboardUI;
    [SerializeField]
    private TextMeshProUGUI tLeaderboardTitle;

    [SerializeField]
    private GameObject lsRankExample;
    [SerializeField]
    private GameObject tRankExample;
    private List<GameObject> lsRanks = new List<GameObject>();
    private List<GameObject> tRanks = new List<GameObject>();
    [SerializeField]
    private GameObject lsUsernameExample;
    [SerializeField]
    private GameObject tUsernameExample;
    private List<GameObject> lsUsernames = new List<GameObject>();
    private List<GameObject> tUsernames = new List<GameObject>();
    [SerializeField]
    private GameObject lsTimeExample;
    [SerializeField]
    private GameObject tTimeExample;
    private List<GameObject> lsTimes = new List<GameObject>();
    private List<GameObject> tTimes = new List<GameObject>();
    

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    public async void ShowLevelSelectLeaderboard(Level level)
    {
        lsLeaderboardUI.SetActive(true);
        lsLeaderboardTitle.text = "World " + level.world + " Level " + level.level + Environment.NewLine + "Leaderboard";
        if (!Settings.instance.online)
        {
            lsLeaderboardTitle.text = "World " + level.world + " Level " + level.level + Environment.NewLine + "Leaderboard"
                + Environment.NewLine + Environment.NewLine + "Offline";
            lsLeaderboardTitle.transform.localPosition = Vector3.up * 32;
            return;
        }

        List<LeaderboardEntry> entries = await GetTopPlayers(level, 20);
        if (entries == null)
        {
            lsLeaderboardTitle.text = "World " + level.world + " Level " + level.level + Environment.NewLine + "Leaderboard"
                + Environment.NewLine + Environment.NewLine + "No entries";
            lsLeaderboardTitle.transform.localPosition = Vector3.up * 32;
            return;
        }
        else 
        {
            lsLeaderboardTitle.transform.localPosition = Vector3.up * 53;
        }
        // float vertOffset = 1080 / Screen.height;
        float vertOffset = 1;
        GameObject rank;
        TextMeshProUGUI rankText;
        GameObject username;
        TextMeshProUGUI usernameText;
        GameObject time;
        TextMeshProUGUI timeText;
        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            rank = lsRanks.Count > i ? lsRanks[i] : null;
            if (rank == null) 
            {
                rank = Instantiate(lsRankExample, lsRankExample.transform.position + (Vector3.down * i * 43 * vertOffset),
                    Quaternion.identity, lsRankExample.transform.parent);
                lsRanks.Add(rank);
            }
            rankText = rank.GetComponent<TextMeshProUGUI>();
            rankText.text = "#" + (i + 1);
            rankText.enabled = true;

            username = lsUsernames.Count > i ? lsUsernames[i] : null;
            if (username == null)
            {
                username = Instantiate(lsUsernameExample, lsUsernameExample.transform.position + (Vector3.down * i * 43 * vertOffset),
                    Quaternion.identity, lsUsernameExample.transform.parent);
                lsUsernames.Add(username);
            }
            usernameText = username.GetComponent<TextMeshProUGUI>();
            usernameText.text = entry.DisplayName;
            usernameText.enabled = true;

            time = lsTimes.Count > i ? lsTimes[i] : null;
            if (time == null)
            {
                time = Instantiate(lsTimeExample, lsTimeExample.transform.position + (Vector3.down * i * 43 * vertOffset),
                    Quaternion.identity, lsTimeExample.transform.parent);
                lsTimes.Add(time);
            }
            timeText = time.GetComponent<TextMeshProUGUI>();
            float timeAmount = entry.Time;
            if (timeAmount % 60 < 10) 
                timeText.text = (int)timeAmount / 60 + ":0" + timeAmount % 60;
            else 
                timeText.text = (int)timeAmount / 60 + ":" + timeAmount % 60;
            timeText.enabled = true;
        }
    }

    public async void ShowTransitionLeaderboard(Level level)
    {
        tLeaderboardUI.SetActive(true);
        tLeaderboardTitle.text = "World " + level.world + " Level " + level.level + Environment.NewLine + "Leaderboard";
        if (!Settings.instance.online)
        {
            tLeaderboardTitle.text = "World " + level.world + " Level " + level.level + Environment.NewLine + "Leaderboard"
                + Environment.NewLine + Environment.NewLine + "Offline";
            tLeaderboardTitle.transform.localPosition = Vector3.up * 32;
            return;
        }

        List<LeaderboardEntry> entries = await GetTopPlayers(level, 20);
        if (entries == null)
        {
            tLeaderboardTitle.text = "World " + level.world + " Level " + level.level + Environment.NewLine + "Leaderboard"
                + Environment.NewLine + Environment.NewLine + "No entries";
            tLeaderboardTitle.transform.localPosition = Vector3.up * 32;
            return;
        }
        else 
        {
            tLeaderboardTitle.transform.localPosition = Vector3.up * 53;
        }
        // float vertOffset = 1080 / Screen.height;
        float vertOffset = 1;
        GameObject rank;
        TextMeshProUGUI rankText;
        GameObject username;
        TextMeshProUGUI usernameText;
        GameObject time;
        TextMeshProUGUI timeText;
        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            rank = tRanks.Count > i ? tRanks[i] : null;
            if (rank == null) 
            {
                rank = Instantiate(tRankExample, tRankExample.transform.position + (Vector3.down * i * 43 * vertOffset),
                    Quaternion.identity, tRankExample.transform.parent);
                tRanks.Add(rank);
            }
            rankText = rank.GetComponent<TextMeshProUGUI>();
            rankText.text = "#" + (i + 1);
            rankText.enabled = true;

            username = tUsernames.Count > i ? tUsernames[i] : null;
            if (username == null)
            {
                username = Instantiate(tUsernameExample, tUsernameExample.transform.position + (Vector3.down * i * 43 * vertOffset),
                    Quaternion.identity, tUsernameExample.transform.parent);
                tUsernames.Add(username);
            }
            usernameText = username.GetComponent<TextMeshProUGUI>();
            usernameText.text = entry.DisplayName;
            usernameText.enabled = true;

            time = tTimes.Count > i ? tTimes[i] : null;
            if (time == null)
            {
                time = Instantiate(tTimeExample, tTimeExample.transform.position + (Vector3.down * i * 43 * vertOffset),
                    Quaternion.identity, tTimeExample.transform.parent);
                tTimes.Add(time);
            }
            timeText = time.GetComponent<TextMeshProUGUI>();
            float timeAmount = entry.Time;
            if (timeAmount % 60 < 10) 
                timeText.text = (int)timeAmount / 60 + ":0" + timeAmount % 60;
            else 
                timeText.text = (int)timeAmount / 60 + ":" + timeAmount % 60;
            timeText.enabled = true;
        }
    }

    public void HideLevelSelectLeaderboard()
    {
        foreach (GameObject rank in lsRanks)
        {
            if (rank != null)
                rank.GetComponent<TextMeshProUGUI>().enabled = false;
        }

        foreach (GameObject username in lsUsernames)
        {
            if (username != null)
                username.GetComponent<TextMeshProUGUI>().enabled = false;
        }

        foreach (GameObject time in lsTimes)
        {
            if (time != null)
                time.GetComponent<TextMeshProUGUI>().enabled = false;
        }
        lsLeaderboardUI.SetActive(false);
    }

    public void HideTransitionLeaderboard()
    {
        foreach (GameObject rank in tRanks)
        {
            if (rank != null)
                rank.GetComponent<TextMeshProUGUI>().enabled = false;
        }

        foreach (GameObject username in tUsernames)
        {
            if (username != null)
                username.GetComponent<TextMeshProUGUI>().enabled = false;
        }

        foreach (GameObject time in tTimes)
        {
            if (time != null)
                time.GetComponent<TextMeshProUGUI>().enabled = false;
        }
        tLeaderboardUI.SetActive(false);
    }

    public async void SubmitTimeAsync(Level level, float time)
    {   
        if (Settings.instance.participateInLeaderboard && Settings.instance.online)
        {
            try 
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(level.ToString(), time);
            }
            catch (Exception e) 
            {
                Debug.LogError($"Failed to submit score: {e}");
                throw;
            }
        }
    }

    public async Task<LeaderboardEntry> GetWorldRecord(Level level)
    {
        if (!Settings.instance.participateInLeaderboard || !Settings.instance.online)
        {
            // Debug.LogWarning("Leaderboard is not enabled or not online.");
            return new LeaderboardEntry { DisplayName = "Not Online", Time = float.PositiveInfinity };
        }
        string levelTitle = "W" + level.world + "L" + level.level;
        try
        {
            var leaderboardResponse = await LeaderboardsService.Instance.GetScoresByTierAsync(levelTitle, "Purple");
            string response = JsonConvert.SerializeObject(leaderboardResponse.Results);
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
            Debug.LogError("Failed to retrieve leaderboard: " + e.Message);
            return new LeaderboardEntry { DisplayName = "No scores", Time = float.PositiveInfinity };
        }
    }

    public async Task<List<LeaderboardEntry>> GetTopPlayers(Level level, int howMany = 10)
    {
        if (!Settings.instance.participateInLeaderboard || !Settings.instance.online)
        {
            Debug.LogWarning("Leaderboard is not enabled or not online.");
            return null;
        }
        Debug.Assert(level != null, "Level cannot be null");
        Debug.Assert(howMany > 0, "howMany must be greater than 0");

        string levelTitle = "W" + level.world + "L" + level.level;
        try
        {
            var leaderboardResponse = await LeaderboardsService.Instance.GetScoresAsync(levelTitle,
                new GetScoresOptions
                {
                    Offset = 0, Limit = howMany
                });
            string response = JsonConvert.SerializeObject(leaderboardResponse.Results);
            if (response == "[]")
            {
                Debug.Log("No scores found for this level.");
                return null;
            }
            else
            {
                List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();
                string name = "";
                string score = "";
                foreach (string entry in response.Split('{'))
                {
                    if (!entry.Contains(',') || !entry.Contains(':'))
                        continue;
                    name = entry.Split(',')[1].Split(':')[1].Replace("\"", "");
                    score = entry.Split(',')[3].Split(':')[1];
                    leaderboardEntries.Add(new LeaderboardEntry { DisplayName = name, Time = float.Parse(score) });
                }
                return leaderboardEntries;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to retrieve leaderboard: " + e.Message);
            return null;
        }
    }

    // public async void GetPlayerRank(Level level, string playerName)
    // {
    //     string levelTitle = "W" + level.world + "L" + level.level;
    //     try
    //     {
    //         var leaderboardEntries = await LeaderboardsService.Instance.GetPlayerRangeAsync(levelTitle, 100); // Get top 100 players

    //         int rank = -1;
    //         for (int i = 0; i < leaderboardEntries.Count; i++)
    //         {
    //             if (leaderboardEntries[i].DisplayName == playerName)
    //             {
    //                 rank = i + 1; // Rank is index + 1 (because rank starts at 1)
    //                 break;
    //             }
    //         }

    //         if (rank > 0)
    //         {
    //             Debug.Log($"{playerName} is ranked #{rank} on the leaderboard.");
    //         }
    //         else
    //         {
    //             Debug.Log($"{playerName} is not in the top 100.");
    //         }
    //     }
    //     catch (System.Exception e)
    //     {
    //         Debug.LogError("Failed to get player rank: " + e.Message);
    //     }
    // }

}

[System.Serializable]
public class LeaderboardTierColorset
{
    public Color normalColor = Color.white;
    public Color highlightedColor = Color.white;
    public Color pressedColor = Color.white;
    public Color selectedColor = Color.white;
    public Color trophyNormalColor = Color.white;
    public Color trophyHighlightedColor = Color.white;
    public Color trophyPressedColor = Color.white;
    public Color trophySelectedColor = Color.white;
    public Color textColor = Color.black;
}
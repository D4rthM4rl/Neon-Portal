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

    public GameObject leaderboardUI;
    [SerializeField]
    private TextMeshProUGUI leaderboardTitle;

    [SerializeField]
    private GameObject rankExample;
    private List<GameObject> ranks = new List<GameObject>();
    [SerializeField]
    private GameObject usernameExample;
    private List<GameObject> usernames = new List<GameObject>();
    [SerializeField]
    private GameObject timeExample;
    private List<GameObject> times = new List<GameObject>();
    

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    public async void ShowLeaderboard(Level level)
    {
        leaderboardUI.SetActive(true);
        leaderboardTitle.text = "World " + level.world + " Level " + level.level + Environment.NewLine + "Leaderboard";
        
        List<LeaderboardEntry> entries = await GetTopPlayers(level);
        if (entries == null)
        {
            leaderboardTitle.text = "World " + level.world + " Level " + level.level + Environment.NewLine + "Leaderboard"
                + Environment.NewLine + Environment.NewLine + "No entries";
            leaderboardTitle.transform.localPosition = Vector3.up * 32;
            return;
        }
        else 
        {
            leaderboardTitle.transform.localPosition = Vector3.up * 53;;
        }
        GameObject rank;
        TextMeshProUGUI rankText;
        GameObject username;
        TextMeshProUGUI usernameText;
        GameObject time;
        TextMeshProUGUI timeText;
        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            rank = ranks.Count > i ? ranks[i] : null;
            if (rank == null) 
            {
                rank = Instantiate(rankExample, rankExample.transform.position + (Vector3.down * i * 25),
                    Quaternion.identity, rankExample.transform.parent);
                ranks.Add(rank);
            }
            rankText = rank.GetComponent<TextMeshProUGUI>();
            rankText.text = "#" + (i + 1);
            rankText.enabled = true;

            username = usernames.Count > i ? usernames[i] : null;
            if (username == null)
            {
                username = Instantiate(usernameExample, usernameExample.transform.position + (Vector3.down * i * 25),
                    Quaternion.identity, usernameExample.transform.parent);
                usernames.Add(username);
            }
            usernameText = username.GetComponent<TextMeshProUGUI>();
            usernameText.text = entry.DisplayName;
            usernameText.enabled = true;

            time = times.Count > i ? times[i] : null;
            if (time == null)
            {
                time = Instantiate(timeExample, timeExample.transform.position + (Vector3.down * i * 25),
                    Quaternion.identity, timeExample.transform.parent);
                times.Add(time);
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

    public void HideLeaderboard()
    {
        foreach (GameObject rank in ranks)
        {
            if (rank != null)
                rank.GetComponent<TextMeshProUGUI>().enabled = false;
        }

        foreach (GameObject username in usernames)
        {
            if (username != null)
                username.GetComponent<TextMeshProUGUI>().enabled = false;
        }

        foreach (GameObject time in times)
        {
            if (time != null)
                time.GetComponent<TextMeshProUGUI>().enabled = false;
        }
        leaderboardUI.SetActive(false);
    }

    public async void SubmitTimeAsync(Level level, float time)
    {   
        if (Settings.instance.participateInLeaderboard)
        {
            try 
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(level.ToString(), time);
                Debug.Log($"Score submitted for {level.ToString()}: {time}");
            }
            catch (Exception e) 
            {
                // messageText.text = $"Failed to submit score: {e}";
                Debug.LogError($"Failed to submit score: {e}");
                throw;
            }
        }
    }

    public async Task<LeaderboardEntry> GetWorldRecord(Level level)
    {
        string levelTitle = "W" + level.world + "L" + level.level;
        try
        {
            var leaderboardResponse = await LeaderboardsService.Instance.GetScoresByTierAsync(levelTitle, "Purple");
            string response = JsonConvert.SerializeObject(leaderboardResponse.Results);
            string name = "";
            string score = "";
            Debug.Log(response);
            if (response == "[]")
            {
                Debug.Log("No scores found for this level.");
                return new LeaderboardEntry { DisplayName = "No scores", Time = float.PositiveInfinity };
            }
            else
            {
                name = response.Split(',')[1].Split(':')[1].Replace("\"", "");
                score = response.Split(',')[3].Split(':')[1];
                Debug.Log("Score: " + score + " by "  + name);
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
                    Debug.Log("Score: " + score + " by "  + name);
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
    public Color textColor = Color.black;
}
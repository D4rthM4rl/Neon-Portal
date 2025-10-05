using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardRow : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI timeText;

    /// <summary>
    /// Change the contents of this leaderboard row.
    /// </summary>
    /// <param name="entry">Leaderboard entry for this to use the data of.</param>
    /// <param name="c">Color to optionally change the text to.</param>
    public void Change(LeaderboardEntry entry, Color c)
    {
        rankText.text = "" + entry.Rank;
        rankText.color = c;
        usernameText.text = entry.DisplayName;
        usernameText.color = c;
        float time = entry.Time;
        if (time % 60 < 10) // For stuff like 1:05 instead of 1:5
            timeText.text = (int)time / 60 + ":0" + time % 60;
        else 
            timeText.text = (int)time / 60 + ":" + time % 60;
        timeText.color = c;
    }

    /// <summary>
    /// Change the contents of this leaderboard row.
    /// </summary>
    /// <param name="entry">Leaderboard entry for this to use the data of.</param>
    public void Change(LeaderboardEntry entry)
    {
        rankText.text = "" + entry.Rank;
        usernameText.text = entry.DisplayName;
        float time = entry.Time;
        if (time % 60 < 10) // For stuff like 1:05 instead of 1:5
            timeText.text = (int)time / 60 + ":0" + time % 60;
        else 
            timeText.text = (int)time / 60 + ":" + time % 60;
    }
}

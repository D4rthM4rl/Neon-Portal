using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
/// <summary>
/// A game level, identified by world and level numbers. Also tracks best time
/// and whether it's been beaten.
/// </summary>
[System.Serializable]
public class Level
{
    public int world;
    public int level;

    [NonSerialized] public StarTiers stars;

    /// <summary>My best time (in sec) on the level.</summary>
    [HideInInspector] public float bestTime = float.PositiveInfinity;
    /// <summary>Whether I've been the level before.</summary>
    [NonSerialized] public bool beaten;
    /// <summary>Total time spent in this level across all attempts.</summary>
    [HideInInspector] public float totalTime = 0;
    /// <summary>Total deaths in this level across all attempts.</summary>
    [HideInInspector] public float totalDeaths = 0;
    /// <summary>Total resets in this level across all attempts.</summary>
    [HideInInspector] public float totalResets = 0;

    /// <summary>List of the top 20 entries/times on the leaderboard for this level.</summary>
    [NonSerialized] public List<LeaderboardEntry> top20;
    /// <summary>List of the 10 entries/times around me on the leaderboard for this level.</summary>
    [NonSerialized] public List<LeaderboardEntry> myRanks;
    public Level(int world, int level)
    {
        this.world = world;
        this.level = level;
        this.bestTime = float.PositiveInfinity;
        this.beaten = false;
        top20 = new List<LeaderboardEntry>();
        myRanks = new List<LeaderboardEntry>();
    }

    public void GetSavedValues()
    {
        // Make sure we have the correct reference
        Level l = LevelSelect.instance.levels[this.world - 1, this.level - 1];

        string title = ToString();

        // Load saved values if they exist, aka if the level has been beaten
        if (PlayerPrefs.HasKey(title))
        {
            l.bestTime = PlayerPrefs.GetFloat(title, float.PositiveInfinity);
            l.beaten = true;
            l.totalTime = PlayerPrefs.GetFloat(title + "TotalTime", l.bestTime);
            l.totalDeaths = PlayerPrefs.GetInt(title + "Deaths", 0);
            l.totalResets = PlayerPrefs.GetInt(title + "Resets", 0);
        }
    }

    public async void AwaitLeaderboardData(Level l)
    {
        int numTries = 0;
        while (numTries < 10 && !OnlineServices.online)
        {
            await Task.Delay(10);
            numTries++;
        }
        if (OnlineServices.online)
        {
            l.top20 = await Leaderboard.instance.GetTopPlayers(l, 20);
            l.myRanks = await Leaderboard.instance.GetMyRanks(l);
        }
    }


    /// <summary>Saves a level completion, saving best time and new total time spent on it.</summary>
    /// <param name="time">Time spent on this completion.</param>
    public void SaveCompletion(float time)
    {
        // Make sure we have the correct reference
        Level l = LevelSelect.instance.levels[this.world - 1, this.level - 1];

        // Best time is just saved as level title
        string bestTitle = ToString();
        bestTime = Mathf.Min(bestTime, time);
        PlayerPrefs.SetFloat(bestTitle, bestTime);

        string totalTimeTitle = ToString() + "TotalTime";
        float totalTime = PlayerPrefs.GetFloat(totalTimeTitle, 0) + time;
        PlayerPrefs.SetFloat(totalTimeTitle, totalTime);
        PlayerPrefs.Save();
        l.totalTime = totalTime;
    }

    /// <summary>Increments the total deaths in this level.</summary>
    /// <param name="time">Time at which this death occurred.</param>
    public void SaveDeath(float time)
    {
        // Make sure we have the correct reference
        Level l = LevelSelect.instance.levels[this.world - 1, this.level - 1];

        string totalTimeTitle = ToString() + "TotalTime";
        l.totalTime = PlayerPrefs.GetFloat(totalTimeTitle, 0) + time;
        PlayerPrefs.SetFloat(totalTimeTitle, l.totalTime);

        string deathTitle = ToString() + "Deaths";
        int deaths = PlayerPrefs.GetInt(deathTitle, 0);
        PlayerPrefs.SetInt(deathTitle, deaths + 1);
        PlayerPrefs.Save();
        l.totalDeaths = deaths + 1;
    }

    /// <summary>Increments the total resets in this level.</summary>
    /// <param name="time">Time at which this reset occurred.</param>
    public void SaveReset(float time)
    {
        // Make sure we have the correct reference
        Level l = LevelSelect.instance.levels[this.world - 1, this.level - 1];

        string totalTimeTitle = ToString() + "TotalTime";
        l.totalTime = PlayerPrefs.GetFloat(totalTimeTitle, 0) + time;
        PlayerPrefs.SetFloat(totalTimeTitle, l.totalTime);

        string resetTitle = ToString() + "Resets";
        int resets = PlayerPrefs.GetInt(resetTitle, 0);
        PlayerPrefs.SetInt(resetTitle, resets + 1);
        PlayerPrefs.Save();
        l.totalResets = resets + 1;
    }

    /// <summary>String representation of a level in form "W" + world + "L" + level.</summary>
	public override string ToString()
	{
		return "W" + world + "L" + level;
	}
}

/// <summary>Tiers of best times for a level with corresponding colors and stars.</summary>
[Serializable] public class StarTiers
{
    /// <summary>Which level these tiers are for.</summary>
    public Level level;
    /// <summary>Top tier of time in sec (3 stars).</summary>
    public float greenTime;
    /// <summary>Mid tier of time in sec (2 stars).</summary>
    public float blueTime;
    /// <summary>Last tier of time in sec (1 star).</summary>
    public float purpleTime;

    /// <summary>Returns how many stars a user has gotten in a level.</summary>
    /// <param name="level">Level to get the user's tier in.</param>
    /// <param name="time">Time to check the tier of.</param>
    /// <returns>3 if 3 stars, -1 if level is unbeaten.</returns>
    public static int GetStarTier(Level level, float time)
    {
        level = LevelSelect.instance.levels[level.world - 1, level.level - 1];
        Debug.Assert(level.stars != null, level + " has no star tiers set");
        if (time < level.stars.greenTime)
        { return 3; }
        if (time < level.stars.blueTime)
        { return 2; }
        if (time < level.stars.purpleTime)
        { return 1; }
        return level.beaten ? 0 : -1;
    }
}
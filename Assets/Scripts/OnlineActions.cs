using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Newtonsoft.Json;
using Unity.Services.Analytics;

/// <summary>
/// Custom class of methods which need internet to work. If these methods fail,
/// then it disables online functionality attempts.
/// </summary>
public class OnlineServices : MonoBehaviour
{
    /// <summary>
    /// Whether we have access to online services such as unity analytics or leaderboard.
    /// </summary>
    public static bool online;


    /// <summary>
    /// Tries to connect to Unity Services and sign in the player anonymously.
    /// Sets online to true if successful, false otherwise.
    /// </summary>
    /// <returns>True if it succeeded, false otherwise.</returns>
    public static async Task<bool> TryToGoOnline()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            online = true;
            return true;
        }
        catch (System.Exception e)
        {
            if (e.Message.Contains("Network Error:"))
            {
                Debug.LogWarning("Failed to go online: " + e.Message);
                online = false;
                return false;
            }
            online = true;
            LevelSelect.instance.ReloadAllLevels();
            return true;
        }
    }

    /// <summary>
    /// Gets the leaderboard response from the Leaderboards Service and returns
    /// it or if it failed, returns null and sets online to false.
    /// </summary>
    /// <param name="leaderboardID">ID of the leaderboard.</param>
    /// <param name="score">Score (time) to add to the leaderboard.</param>
    public async static void AddPlayerScore(string leaderboardID, double score)
    {
        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardID, score);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to add player score to leaderboard: " + e.Message + " Going offline.");
            online = false;
        }
    }

    /// <summary>
    /// Gets the leaderboard response from the Leaderboards Service and returns
    /// it or if it failed, returns null and sets online to false.
    /// </summary>
    /// <param name="leaderboardID">ID of the leaderboard.</param>
    /// <param name="tierID">ID of the tier.</param>
    /// <returns>Serialized string of response results or null.</returns>
    public async static Task<string> GetScoresByTier(string leaderboardID, string tierID)
    {
        try
        {
            var leaderboardResponse = await LeaderboardsService.Instance.GetScoresByTierAsync(leaderboardID, tierID);
            return JsonConvert.SerializeObject(leaderboardResponse.Results);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to retrieve leaderboard: " + e.Message + " Going offline.");
            Settings.instance.ErrorOnline();
            online = false;
            return null;
        }
    }

    /// <summary>
    /// Records an event with Unity Analytics. If it fails, it sets online to false.
    /// </summary>
    /// <param name="e">Event to record.</param>
    public static void RecordEvent(Unity.Services.Analytics.Event e)
    {
        try
        {
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to record Unity Event: " + ex.Message + " Going offline.");
            online = false;
        }
    }

    /// <summary>
    /// Gets some amount of scores from the leaderboard.
    /// Sets online to false if it fails.
    /// </summary>
    /// <param name="leaderboardID">The ID (from Unity website)of the leaderboard to get.</param>
    /// <param name="options">GetScoresOptions to modify what scores to get from the leaderboard.</param>
    /// <returns>String serialized from the leaderboard results.</returns>
    public async static Task<string> GetScores(string leaderboardID, GetScoresOptions options = null)
    {
        try
        {
            var leaderboardResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardID, options);
            return JsonConvert.SerializeObject(leaderboardResponse.Results);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to retrieve scores: " + e.Message + " Going offline.");
            online = false;
            return null;
        }
    }

    /// <summary>
    /// Gets some amount of scores from the leaderboard around the player.
    /// Sets online to false if it fails.
    /// </summary>
    /// <param name="leaderboardID">The ID (from Unity website)of the leaderboard to get.</param>
    /// <param name="options">GetPlayerRangeOptions to modify what scores to get from the leaderboard.</param>
    /// <returns>String serialized from the leaderboard results.</returns>
    public async static Task<string> GetPlayerRangeAsync(string leaderboardID, GetPlayerRangeOptions options = null)
    {
        try
        {
            var leaderboardResponse = await LeaderboardsService.Instance.GetPlayerRangeAsync(leaderboardID, options);
            return JsonConvert.SerializeObject(leaderboardResponse.Results);
        }
        catch (System.Exception e)
        {
            Debug.Log("Level: " + leaderboardID);
            if (e.Message.Contains("Leaderboard entry could not be found"))
            return "Unbeaten online";
            Debug.LogWarning("Failed to retrieve scores around player: " + e.Message + " Going offline." + e.Data);
            online = false;
            return null;
        }
    }

    /// <summary>
    /// Gets the player's name from the Authentication services. Sets online to
    /// to false if it fails.
    /// </summary>
    /// <returns></returns>
    public async static Task<string> GetPlayerName()
    {
        try
        {
            return await AuthenticationService.Instance.GetPlayerNameAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to get player name: " + ex.Message + " Going offline.");
            online = false;
            return null;
        }
    }

    /// <summary>
    /// Updates the player's name in the authentication service or sets online
    /// to false if it fails.
    /// </summary>
    /// <param name="name">What to update the player's name to.</param>
    public async static void UpdatePlayerName(string name)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to update player name: " + ex.Message + " Going offline.");
            online = false;
        }
    }

    /// <summary>
    /// Starts or stops collecting data using analytics service or sets
    /// online to false if it fails.
    /// </summary>
    /// <param name="collect">Whether to collect data or not.</param>
    public static void ChangeDataCollection(bool collect)
    {
        try
        {
            if (collect) AnalyticsService.Instance.StartDataCollection();
            else AnalyticsService.Instance.StopDataCollection();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to change data collection: " + ex.Message + " Going offline.");
            online = false;
        }
    }
    
    /// <summary>
    /// Requests the analytics service to delete the collected data or sets
    /// online to false if it fails.
    /// </summary>
    public static void RequestDataDeletion()
    {
        try
        {
            AnalyticsService.Instance.RequestDataDeletion();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to request data deletion: " + ex.Message + " Going offline.");
            online = false;
        }
    }
}

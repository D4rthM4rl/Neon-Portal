using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.CloudSave;

public class ExitDoor : MonoBehaviour
{
    public int currWorld;
    public int currLevel;

    private void OnTriggerStay2D(Collider2D other) {
        Player player = other.GetComponent<Player>();
        if (player && player.isGrounded)
        {
            BeatLevel(player, Timer.instance.timer);
            if (GetNextLevel() == "Home")
            {
                MainMenu.instance.gameObject.SetActive(true);
                Timer.instance.timerText.enabled = false;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
            }
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(GetNextLevel());
        }
    }

    async private void BeatLevel(Player player, float time)
    {
        Level level = new Level(currWorld, currLevel);
        // Send an event to Unity Analytics when the player completes a level
        string levelTitle = "W" + currWorld + "L" + currLevel;
        level_complete levelCompleteEvent = new level_complete
        {
            level = levelTitle,
            num_deaths = player.numDeaths,
            num_resets = player.numResets,
            timer = time
        };
        AnalyticsService.Instance.RecordEvent(levelCompleteEvent);

        Leaderboard.instance.SubmitTimeAsync(level, time);

        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{levelTitle});
        float bestTime = float.PositiveInfinity;
        Debug.Log("Beat " + levelTitle + " in " + time + " seconds");
        if (playerData.TryGetValue(levelTitle, out var levelTime))
        {
            bestTime = float.Parse(levelTime.Value.GetAs<string>());
            Debug.Log($"Best time for {levelTitle}: {bestTime}");
        }

        if (time < bestTime)
        {
            var saveBestTime = new Dictionary<string, object>{ { levelTitle, time } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(saveBestTime);
            Debug.Log($"New best time for {levelTitle}: {time}");



            
            if (LevelSelect.instance == null)
            {
                Debug.Log("LevelSelect instance is null");
            }
            else
            {
                LevelSelect.instance.levels[currWorld - 1, currLevel - 1].bestTime = time;
                LevelSelect.instance.levels[currWorld - 1, currLevel - 1].beaten = true;
                level.bestTime = time;
                LevelSelect.instance.levelsToReload.Add(level);
            }
        }
    }

    /// <summary>
    /// Gets either the next level in current world or first level of the next world
    /// </summary>
    /// <returns>Next level</returns>
    private string GetNextLevel()
    {
        if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("W" + currWorld + "L" + (currLevel + 1)) != -1)
        {
            return "W" + currWorld + "L" + (currLevel + 1);
        }
        else if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("W" + (currWorld + 1) + "L1") != -1)
        {
            return "W" + (currWorld + 1) + "L1";
        }
        else
        {
            // Debug.LogError("No next level found for " + "W" + currWorld + "L" + (currLevel + 1)
            //  + " or W" + (currWorld + 1) + "L1");
            
            return "Home";
        }
    }
}

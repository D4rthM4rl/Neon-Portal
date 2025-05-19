using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Analytics;

public class ExitDoor : MonoBehaviour
{
    public int currWorld;
    public int currLevel;

    private void OnTriggerStay2D(Collider2D other) {
        Player player = other.GetComponent<Player>();
        if (player && player.isGrounded)
        {
            BeatLevel(player);
            player.numDeaths = 0;
            player.numResets = 0;
            // Load the next scene
            player.ResetPlayer();
            player.ResetPortals();
            UnityEngine.SceneManagement.SceneManager.LoadScene(GetNextLevel());
            if (GetNextLevel() == "Home")
            {
                player.GoHome();
            }
        }
    }

    private void BeatLevel(Player player)
    {
        Level level = new Level(currWorld, currLevel);
        if (player.timer < PlayerPrefs.GetFloat("W" + currWorld + "L" + currLevel, float.PositiveInfinity))
        {
            PlayerPrefs.SetFloat("W" + currWorld + "L" + currLevel, player.timer);
            PlayerPrefs.Save();
            
            // Send an event to Unity Analytics when the player completes a level
            level_complete levelCompleteEvent = new level_complete
            {
                level = "W" + currWorld + "L" + currLevel,
                num_deaths = player.numDeaths,
                num_resets = player.numResets,
                timer = player.timer
            };
            AnalyticsService.Instance.RecordEvent(levelCompleteEvent);
            
            if (LevelSelect.instance == null)
            {
                Debug.Log("LevelSelect instance is null");
            }
            else
            {
                LevelSelect.instance.levels[currWorld - 1, currLevel - 1].bestTime = player.timer;
                LevelSelect.instance.levels[currWorld - 1, currLevel - 1].beaten = true;
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

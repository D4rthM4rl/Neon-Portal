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
            BeatLevel(player, Timer.instance.levelTimer, Timer.instance.unresetLevelTimer);
            string nextLevel = GetNextLevel();
            if (nextLevel == "Home")
            {
                MainMenu.instance.gameObject.SetActive(true);
                Timer.instance.timerText.enabled = false;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevel);
            }
        }
    }

    async private void BeatLevel(Player player, float levelTimer, float unresetLevelTimer)
    {
        Level level = LevelSelect.instance.levels[currWorld - 1, currLevel - 1];
        // Send an event to Unity Analytics when the player completes a level
        string levelTitle = "W" + currWorld + "L" + currLevel;
        
        RecordLevelCompleteEvent(level, player, levelTimer, unresetLevelTimer);
        Leaderboard.instance.SubmitTimeAsync(level, levelTimer);

        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{levelTitle});
        float bestTime = float.PositiveInfinity;
        if (playerData.TryGetValue(levelTitle, out var levelTime))
        {
            bestTime = float.Parse(levelTime.Value.GetAs<string>());
            Debug.Log($"Best time for {levelTitle}: {bestTime}");
        }

        if (levelTimer < bestTime)
        {
            var saveBestTime = new Dictionary<string, object>{ { levelTitle, levelTimer } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(saveBestTime);
            Debug.Log($"New best time for {levelTitle}: {levelTimer}");



            
            if (LevelSelect.instance == null)
            {
                Debug.Log("LevelSelect instance is null");
            }
            else
            {
                level.bestTime = levelTimer;
                level.beaten = true;
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

    private void RecordLevelCompleteEvent(Level level, Player player, float levelTimer, float unresetLevelTimer)
    {
        level_complete levelCompleteEvent = new level_complete
        {
            level = level.ToString(),
            level_beaten = level.beaten,
            num_deaths = player.numDeaths,
            num_resets = player.numResets,
            timer = levelTimer,
            unreset_timer = unresetLevelTimer
        };

        if (PortalGun.portalsInScene.Length > 0 && PortalGun.portalsInScene[0] != null)
        {
            Vector3 portalPos = PortalGun.portalsInScene[0].transform.position;
            levelCompleteEvent.portal1_x = portalPos.x;
            levelCompleteEvent.portal1_y = portalPos.y;
        }
        if (PortalGun.portalsInScene.Length > 1 && PortalGun.portalsInScene[1] != null)
        {
            Vector3 portalPos = PortalGun.portalsInScene[1].transform.position;
            levelCompleteEvent.portal2_x = portalPos.x;
            levelCompleteEvent.portal2_y = portalPos.y;
        }

        AnalyticsService.Instance.RecordEvent(levelCompleteEvent);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitDoor : MonoBehaviour
{
    public int currWorld;
    public int currLevel;
    private bool transitioning = false;

    private void OnTriggerStay2D(Collider2D other) {
        Player player = other.GetComponent<Player>();
        if (player && player.isGrounded && !transitioning)
        {
            transitioning = true;
            player.enabled = false;
            float prevBestTime = BeatLevel(player, Timer.instance.levelTimer, Timer.instance.unresetLevelTimer);
            Transition.instance.StartTransition(currWorld, currLevel, Timer.instance.levelTimer, prevBestTime);
        }
    }

    /// <summary>Saves the level completion data and sends an event to Unity Analytics.</summary>
    /// <param name="player">Player who beat the level.</param>
    /// <param name="levelTimer">The time they got on the level.</param>
    /// <param name="unresetLevelTimer">How long they played the level for, not 
    /// resetting after death or reset.</param>
    /// <returns>Best time for level before this completion.</returns>
    private float BeatLevel(Player player, float levelTimer, float unresetLevelTimer)
    {
        Level level = LevelSelect.instance.levels[currWorld - 1, currLevel - 1];
        string levelTitle = level.ToString();
        
        // Send an event to Unity Analytics when the player completes a level
        RecordLevelCompleteEvent(level, player, levelTimer, unresetLevelTimer);
        Leaderboard.instance.SubmitTime(level, levelTimer);

        float bestTime = PlayerPrefs.GetFloat(levelTitle, float.PositiveInfinity);
        if (levelTimer < bestTime)
        {
            if (LevelSelect.instance == null)
            {
                Debug.LogWarning("LevelSelect instance is null");
            }
            else
            {
                level.bestTime = levelTimer;
                level.beaten = true;
                LevelSelect.instance.levelsToReload.Add(level);
            }
        }
        level.SaveCompletion(levelTimer);
        return bestTime;
    }

    /// <summary>
    /// Record an event in unity analytis to show that a level has been beaten.
    /// </summary>
    /// <param name="level">Level which was beaten.</param>
    /// <param name="player">Player who beat the level.</param>
    /// <param name="levelTimer">Timer when the player beat the level.</param>
    /// <param name="unresetLevelTimer">How long the player was playing the level regardless of deaths/resets.</param>
    private void RecordLevelCompleteEvent(Level level, Player player, float levelTimer, float unresetLevelTimer)
    {
        if (Settings.instance == null || !OnlineServices.online) return;
        level_complete levelCompleteEvent = new level_complete
        {
            level = level.ToString(),
            level_beaten = level.beaten,
            num_deaths = player.numDeaths,
            num_resets = player.numResets,
            timer = levelTimer,
            unreset_timer = unresetLevelTimer,
            movement_type = (int)Settings.instance.movement
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

        OnlineServices.RecordEvent(levelCompleteEvent);
    }
}

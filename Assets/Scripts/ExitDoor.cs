using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Analytics;
using Unity.Services.CloudSave;

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
            BeatLevel(player, Timer.instance.levelTimer, Timer.instance.unresetLevelTimer);
            Transition.instance.StartTransition(currWorld, currLevel, Timer.instance.levelTimer);
        }
    }

    async private void BeatLevel(Player player, float levelTimer, float unresetLevelTimer)
    {
        // TODO: Make it fine to be offlien
        Level level = LevelSelect.instance.levels[currWorld - 1, currLevel - 1];
        string levelTitle = "W" + currWorld + "L" + currLevel;
        
        // Send an event to Unity Analytics when the player completes a level
        RecordLevelCompleteEvent(level, player, levelTimer, unresetLevelTimer);
        Leaderboard.instance.SubmitTimeAsync(level, levelTimer);

        float bestTime = PlayerPrefs.GetFloat(levelTitle, float.PositiveInfinity);

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

    private void RecordLevelCompleteEvent(Level level, Player player, float levelTimer, float unresetLevelTimer)
    {
        if (Settings.instance == null || !Settings.instance.online) return;
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

        AnalyticsService.Instance.RecordEvent(levelCompleteEvent);
    }
}

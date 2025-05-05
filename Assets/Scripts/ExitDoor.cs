using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitDoor : MonoBehaviour
{
    public int currWorld;
    public int currLevel;

    // public int levelIndex = 1;

    // Start is called before the first frame update
    void Awake()
    {
        // if (currWorld == 0 || currLevel == 0)
        // {
        //     Debug.LogError("Current world or level not set for Exit Door.");
        // }
    }

    private void OnTriggerStay2D(Collider2D other) {
        Player player = other.GetComponent<Player>();
        if (player && player.isGrounded)
        {
            // Unload the current scene
            // UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("W" + currWorld + "L" + currLevel);
            // Load the next scene
            player.Reset();
            UnityEngine.SceneManagement.SceneManager.LoadScene(GetNextLevel());
            // Debug.Log(UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(levelIndex));

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
            Debug.LogError("No next level found for " + "W" + currWorld + "L" + (currLevel + 1)
             + " or W" + (currWorld + 1) + "L1");
            return null;
        }
    }
}

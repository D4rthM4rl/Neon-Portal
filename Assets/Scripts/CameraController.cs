using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Sprite backgroundSprite;
    void Start()
    {
        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogError("CinemachineBrain component not found on the camera.");
            return;
        }
        StartCoroutine(GetCamera(brain));
        GetComponentInChildren<SpriteRenderer>().sprite = backgroundSprite;
        if (backgroundSprite == null)
        {
            Debug.LogError("Skybox material not assigned.");
            return;
        }
    }

    private IEnumerator GetCamera(CinemachineBrain brain)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player GameObject with tag 'Player' not found.");
            yield break;
        }
        
        // Wait until the active virtual camera is a CinemachineVirtualCamera
        while (brain.ActiveVirtualCamera == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        brain.ActiveVirtualCamera.Follow = player.transform;
    }
}

using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Sprite backgroundSprite;
    [SerializeField]
    private Material unlitMat;

    void Start()
    {
        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogError("CinemachineBrain component not found on the camera.");
            return;
        }
        StartCoroutine(GetCamera(brain));
        GameObject bg;
        SpriteRenderer bgSR = GetComponentInChildren<SpriteRenderer>();
        if (bgSR != null)
        {
            bg = GetComponentInChildren<SpriteRenderer>().gameObject;
        }
        else
        {
            bg = new GameObject("Background");
            bg.transform.SetParent(transform);
            bg.transform.localPosition = Vector3.forward;
            bg.transform.localScale = new Vector3(1.85f, 1.85f, 1);
            bgSR = bg.AddComponent<SpriteRenderer>();
            if (backgroundSprite == null || unlitMat == null)
            {
                Debug.LogError("Skybox material or unlitMat not assigned.");
                return;
            }
            bgSR.sprite = backgroundSprite;
            bgSR.material = unlitMat;
        }
        bgSR.sortingLayerName = "Sky";
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

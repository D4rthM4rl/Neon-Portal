using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public bool rotateWithGravity = true;
    [SerializeField]
    private GameObject player;
    private ICinemachineCamera virtualCamera;

    private GameObject bg;

    void Start()
    {
        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogError("CinemachineBrain component not found on the camera.");
            return;
        }

        StartCoroutine(GetCamera(brain));
        bg = GetComponentInChildren<SpriteRenderer>().gameObject;
        if (bg == null)
        {
            Debug.LogError("Background GameObject not found.");
            return;
        }
    }

    private IEnumerator GetCamera(CinemachineBrain brain)
    {
        GameObject[] playerTags = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in playerTags)
        {
            if (p.GetComponent<Player>() != null)
            {
                player = p;
                break;
            }
        }

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

        virtualCamera = brain.ActiveVirtualCamera;
        virtualCamera.Follow = player.transform;
    }

    public float cameraRotateSpeed = 360f; // Degrees per second

    void Update()
    {
        if (player != null && virtualCamera != null && Settings.instance.rotateCameraWithGravity)
        {
            Transform virtualCameraTransform = virtualCamera.VirtualCameraGameObject.transform;
            Vector2 grav = player.GetComponent<Player>().gravityDirection.normalized;
            float targetAngle = Vector2.SignedAngle(Vector2.down, grav);

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

            virtualCameraTransform.rotation = Quaternion.RotateTowards(
                virtualCameraTransform.rotation,
                targetRotation,
                cameraRotateSpeed * Time.deltaTime
            );
            // Quaternion backgroundRotation = Quaternion.Euler(0f, 0f, -targetAngle);
            // bg.transform.rotation = backgroundRotation;
        }
    }

}

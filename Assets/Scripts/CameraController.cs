using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    /// <summary>The player GameObject the camera should follow.</summary>
    private GameObject player;
    /// <summary>The currently active Cinemachine virtual camera.</summary>
    [System.NonSerialized] public ICinemachineCamera virtualCamera;
    /// <summary>The currently active Cinemachine virtual camera.</summary>
    [System.NonSerialized] private CinemachineBrain brain;
    /// <summary>
    /// The speed at which the camera rotates to match the player's gravity direction
    /// in degrees per second.
    /// </summary>
    public float cameraRotateSpeed = 360f;

    void Start()
    {
        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogError("CinemachineBrain component not found on the camera.");
            return;
        }
        StartCoroutine(GetCamera(brain));
    }

    /// <summary>
    /// Sets the virtual camera to follow the player once the player and camera are available.
    /// </summary>
    /// <param name="brain">The Cinemachine Brain which controls the camera.</param>
    private IEnumerator GetCamera(CinemachineBrain brain)
    {
        player = Player.instance.gameObject;

        if (player == null)
        {
            Debug.LogError("Player GameObject with tag 'Player' not found.");
            yield break;
        }
        
        // Wait until the active virtual camera is a CinemachineVirtualCamera
        while (brain.ActiveVirtualCamera == null)
        {
            yield return null;
        }

        virtualCamera = brain.ActiveVirtualCamera;
        
        if (!virtualCamera.Follow) virtualCamera.Follow = player.transform;


        float targetAspect = 16f / 9f; // your original design aspect
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;
        CameraState state = virtualCamera.State;
        CinemachineVirtualCamera realVC = virtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
        

        if (scaleHeight > 1.0f)
        {
            realVC.m_Lens.OrthographicSize /= scaleHeight;
        }
    }

    void Update()
    {
        if (player != null && virtualCamera != null && Settings.instance != null && Settings.instance.rotateCameraWithGravity)
        {
            Transform virtualCameraTransform = virtualCamera.VirtualCameraGameObject.transform;
            Vector2 grav = player.GetComponent<Player>().gravityDirection.normalized;
            float targetAngle = Vector2.SignedAngle(Vector2.down, grav);

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

            virtualCameraTransform.rotation = Quaternion.RotateTowards(
                virtualCameraTransform.rotation,
                targetRotation,
                cameraRotateSpeed * Time.unscaledDeltaTime
            );
            // Quaternion backgroundRotation = Quaternion.Euler(0f, 0f, -targetAngle);
            // bg.transform.rotation = backgroundRotation;
        }
    }
}

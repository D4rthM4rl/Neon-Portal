using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public bool rotateWithGravity = true;
    [SerializeField]
    private Material unlitMat;
    private GameObject player;
    private ICinemachineCamera virtualCamera;

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
        }
        bgSR.sortingLayerName = "Sky";
    }

    private IEnumerator GetCamera(CinemachineBrain brain)
    {
        player = GameObject.FindGameObjectWithTag("Player");
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
        if (player != null && virtualCamera != null && rotateWithGravity)
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
        }
    }

}

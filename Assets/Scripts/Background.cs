using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class Background : MonoBehaviour
{
    private Camera cam;
    private RectTransform rectTransform;
    private Vector3 pos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        cam = Camera.main;
        Canvas canvas = GetComponentInParent<Canvas>();
        canvas.worldCamera = cam;

        float height = canvas.GetComponent<RectTransform>().sizeDelta.y;
        float width = canvas.GetComponent<RectTransform>().sizeDelta.x;
        float scale = canvas.GetComponent<CanvasScaler>().referenceResolution.x / width;
        height *= scale;
        width *= scale;

        float diagonal = Mathf.Sqrt(width * width + height * height);

        rectTransform.sizeDelta = new Vector2(diagonal, diagonal);
    }


    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        pos = cam.transform.position;
        pos.z = 0;
        transform.position = pos;
        transform.rotation = Quaternion.identity;
    }
}

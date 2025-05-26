using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SVImageControl : MonoBehaviour, IDragHandler, IPointerClickHandler
{
    [SerializeField]
    private Image pickerImage;

    private RawImage SVimage;

    private ColorPickerControl CC;

    private RectTransform rectTransform;
    private RectTransform pickerTransform;

    
    private void Awake() 
    {
        SVimage = GetComponent<RawImage>();
        CC = FindObjectOfType<ColorPickerControl>();
        rectTransform = GetComponent<RectTransform>();
        pickerTransform = pickerImage.GetComponent<RectTransform>();
        pickerTransform.position = new Vector2(-(rectTransform.sizeDelta.x * 0.5f), -(rectTransform.sizeDelta.y * 0.5f));
    }

    private void UpdateColor(PointerEventData eventData)
    {
        Vector3 pos = rectTransform.InverseTransformPoint(eventData.position);

        float deltaX = rectTransform.sizeDelta.x * 0.5f;
        float deltaY = rectTransform.sizeDelta.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -deltaX, deltaX);
        pos.y = Mathf.Clamp(pos.y, -deltaY, deltaY);

        float x = pos.x + deltaX;
        float y = pos.y + deltaY;

        float xNorm = x / rectTransform.sizeDelta.x;
        float yNorm = y / rectTransform.sizeDelta.y;

        pickerTransform.localPosition = pos;
        pickerImage.color = Color.HSVToRGB(0, 0, 1 - yNorm);
        CC.SetSV(xNorm, yNorm);
    }

    public void SetPickerFromColor(Color color)
    {
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);

        // Compute normalized coordinates
        float xNorm = s;       // Saturation goes left to right
        float yNorm = v;       // Value goes bottom to top

        float deltaX = rectTransform.sizeDelta.x * 0.5f;
        float deltaY = rectTransform.sizeDelta.y * 0.5f;

        // Convert normalized (0-1) to local position
        float x = xNorm * rectTransform.sizeDelta.x - deltaX;
        float y = yNorm * rectTransform.sizeDelta.y - deltaY;

        Vector2 localPos = new Vector2(x, y);

        pickerTransform.localPosition = localPos;
        pickerImage.color = color;

        // Optional: inform controller of change
        CC.SetSV(xNorm, yNorm);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateColor(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UpdateColor(eventData);
    }
}

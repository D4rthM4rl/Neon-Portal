using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

[RequireComponent(typeof(Light2D))]
public class LightOscillator : MonoBehaviour
{
    [Tooltip("How bright the light gets at its peak.")]
    public float maxIntensity = 1.5f;

    [Tooltip("How dim the light gets at its lowest.")]
    public float minIntensity = 0.5f;

    [Tooltip("How fast the light pulses.")]
    public float speed = 2f;

    private Light2D light2D;
    private float baseTime;

    private void Awake()
    {
        if (!TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI text)) light2D = GetComponent<Light2D>();
        // text.material.
        baseTime = Random.Range(0f, Mathf.PI * 2f); // randomize start phase
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.unscaledTime * speed + baseTime) + 1f) / 2f; 
        // maps sine (-1..1) to (0..1)

        light2D.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}

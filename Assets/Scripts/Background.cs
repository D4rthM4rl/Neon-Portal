using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a 3-layer UI parallax background (Sky, Buildings, Windows) built from
/// full-size Images (each sized to the full 12000x4000 source sprite) that are
/// simply repositioned to reveal different portions of the image.
///
///   - Per-layer tint via Image.color.
///   - A per-level base horizontal position: level 0 shows the leftmost
///     viewWidth slice, the last level shows the rightmost slice.
///   - Continuous parallax scrolling as the camera moves within a level, each
///     layer at its own fraction of the camera's speed.
///   - Full 360-degree rotation independence for gravity-flip: backgroundRoot
///     is counter-rotated to stay upright regardless of its parent rig's
///     rotation. Because a 3240x1964 window can never need more than its
///     diagonal (~3789px) of coverage at any rotation angle, and the source
///     image is 12000x4000, a single fixed margin - computed once, not per
///     frame - keeps every level's position safely inside the image at any
///     rotation, with no runtime resizing needed.
///
/// Hierarchy expectation (bottom to top so later siblings draw on top):
///   RotatingRig (rotates for gravity direction)
///     -> BackgroundRoot (RectTransform, counter-rotated by this script)
///          -> Sky (Image, sizeDelta = full sky sprite size)
///          -> Buildings (Image, sizeDelta = full buildings sprite size)
///          -> Windows (Image, sizeDelta = full windows sprite size)
/// </summary>
[ExecuteAlways]
public class ParallaxSkylineBackground : MonoBehaviour
{
    [Header("Rotation Container")]
    [Tooltip("Parent RectTransform of Sky/Buildings/Windows. Counter-rotated to keep the background upright. If empty, this GameObject's own RectTransform is used.")]
    [SerializeField] private RectTransform backgroundRoot;

    [Header("Layer References (Sky behind, Buildings middle, Windows front)")]
    [SerializeField] private Image skyImage;
    [SerializeField] private Image buildingsImage;
    [SerializeField] private Image windowsImage;

    // [SerializeField] private Sprite sky;
    // [SerializeField] private Sprite buildings;
    // [SerializeField] private Sprite windows;

    private Camera cam;

    [Header("Tints (multiplied over each sprite)")]
    [SerializeField] private Color skyTint = Color.white;
    [SerializeField] private Color buildingsTint = Color.white;
    [SerializeField] private Color windowsTint = Color.white;

    [Header("Source Image Dimensions (pixels) - Buildings/Windows")]
    [SerializeField] private int sourceWidth = 12000;
    [SerializeField] private int sourceHeight = 4000;

    [Header("View / Reference Resolution (Canvas Scaler)")]
    [SerializeField] private int viewWidth = 3240;
    [SerializeField] private int viewHeight = 1964;

    [Tooltip("Current live rotation of the rig, in degrees. Drive from your gravity/rotation system via SetRotation(). Full 360-degree rotation is supported.")]
    [SerializeField] private float currentRotationDegrees = 0f;

    [Header("Parallax")]
    [Tooltip("How fast Buildings/Windows scroll relative to camera movement within a level. 0 = fixed in place, 1 = moves exactly with the camera.")]
    [SerializeField, Range(0f, 1f)] private float buildingsParallaxFactor = 0.4f;

    [Tooltip("How fast Sky scrolls, both for in-level parallax and its share of the per-level base position advance.")]
    [SerializeField, Range(0f, 1f)] private float skyParallaxFactor = 0.15f;

    [Tooltip("World units -> UI pixels conversion for camera movement (reference resolution width / visible world width at your camera's orthographic size).")]
    [SerializeField] private float worldToUiPixelScale = 100f;
    
    [SerializeField] private int totalLevels = 24;
    private int levelIndex = 0;

    // The largest a viewWidth x viewHeight window can ever need to be covered
    // at ANY rotation angle - the rectangle's diagonal. Computed once, not per
    // frame: it only depends on the fixed view dimensions.
    private float _rotationSafetyMargin;

    // private void OnEnable() => Initialize();
    // private void OnValidate() => Initialize();

    private void Awake()
    {
        float diagonal = Mathf.Sqrt(viewWidth * viewWidth + viewHeight * viewHeight);
        _rotationSafetyMargin = diagonal * 0.5f;

        // skyImage.sprite = sky;
        // buildingsImage.sprite = buildings;
        // windowsImage.sprite = windows;

        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Level level;
        if (LevelSelect.instance != null)
        {
            level = LevelSelect.instance.GetLevelByName(levelName);
            levelIndex = level.world * 6 + level.level;
        }

        cam = Camera.main;
        GetComponent<Canvas>().worldCamera = cam;

        ApplyTint();
        ApplySizing();
        // backgroundRoot.localRotation = Quaternion.Euler(0f, 0f, -currentRotationDegrees);
        ApplyPositions();
    }

    /// <summary>Call every frame (or whenever the camera moves) with the camera's current world-space X.</summary>
    private void Update()
    {
        ApplyPositions();
    }

    /// <summary>Call whenever the rig's rotation changes.</summary>
    public void SetRotation(float rigRotationDegrees)
    {
        currentRotationDegrees = rigRotationDegrees;
        backgroundRoot.localRotation = Quaternion.Euler(0f, 0f, -currentRotationDegrees);
    }

    private void ApplyTint()
    {
        skyImage.color = skyTint;
        buildingsImage.color = buildingsTint;
        windowsImage.color = windowsTint;
    }

    private void ApplySizing()
    {
        skyImage.rectTransform.sizeDelta = new Vector2(viewWidth, viewHeight);
        buildingsImage.rectTransform.sizeDelta = new Vector2(sourceWidth, sourceHeight);
        windowsImage.rectTransform.sizeDelta = new Vector2(sourceWidth, sourceHeight);
    }

    private void ApplyPositions()
    {
        // Nominal per-level center (source-pixel space): level 0 -> first
        // viewWidth slice, last level -> final viewWidth slice.
        float nominalMinCenter = viewWidth / 2f;
        float nominalMaxCenter = sourceWidth - viewWidth / 2f;
        float t = totalLevels > 1 ? (float)levelIndex / (totalLevels - 1) : 0f;
        t = Mathf.Clamp01(t);
        float levelBaseCenter = Mathf.Lerp(nominalMinCenter, nominalMaxCenter, t);

        // Fixed safety bounds - reserve the rotation diagonal's half-width on
        // both ends so scrolling/rotating never samples past the image edges.
        float safeMinCenter = _rotationSafetyMargin;
        float safeMaxCenter = sourceWidth - _rotationSafetyMargin;

        float cameraDeltaPixels = cam.transform.position.x * worldToUiPixelScale;

        float buildingsCenterX = levelBaseCenter + cameraDeltaPixels * buildingsParallaxFactor;
        buildingsCenterX = Mathf.Clamp(buildingsCenterX, safeMinCenter, safeMaxCenter);
        float buildingsAnchoredX = sourceWidth / 2f - buildingsCenterX;

        SetX(buildingsImage, buildingsAnchoredX);
        SetX(windowsImage, buildingsAnchoredX); // identical to buildings - keeps them pixel-aligned

        // float skyLevelBaseCenter = Mathf.Lerp(nominalMinCenter, nominalMaxCenter, t * skyParallaxFactor);
        // float skyCenterX = skyLevelBaseCenter + cameraDeltaPixels * skyParallaxFactor;
        // skyCenterX = Mathf.Clamp(skyCenterX, safeMinCenter, safeMaxCenter);
        // SetX(skyImage, sourceWidth / 2f - skyCenterX);
        SetX(skyImage, cam.transform.position.x);

        // Vertical stays centered on all layers at all times - the source
        // image's 4000px height already exceeds the ~3789px worst-case
        // diagonal, so no vertical adjustment is ever needed.
    }

    private static void SetX(Image image, float anchoredX)
    {
        Vector2 pos = image.rectTransform.anchoredPosition;
        pos.x = anchoredX;
        pos.y = 0f;
        image.rectTransform.anchoredPosition = pos;
    }
}
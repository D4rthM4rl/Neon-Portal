using UnityEngine;

/// <summary>
/// One glowing edge segment (child of a Block, positioned along one of its
/// four sides). Block toggles this on/off and recolors it depending on
/// whether that side is internal (shared with a same-group neighbor) or
/// exposed (part of the group's outer boundary).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NeonEdge : MonoBehaviour
{
    [Tooltip("Multiplies the outline color so it can push past 1.0 and bloom under URP/HDRP Bloom.")]
    private float glowIntensity = 2f;

    private SpriteRenderer _sr;
    private static MaterialPropertyBlock _mpb;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void SetColor(Color color)
    {
        _mpb ??= new MaterialPropertyBlock();
        _sr.GetPropertyBlock(_mpb);
        _mpb.SetColor("_Color", color * glowIntensity);
        _sr.SetPropertyBlock(_mpb);
    }
}
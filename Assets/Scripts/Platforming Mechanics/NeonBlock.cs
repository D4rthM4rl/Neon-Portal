using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to ANY block prefab (alongside whatever type-specific gameplay
/// script it has) plus a SpriteRenderer + BoxCollider2D. Gives it a neon
/// outline that automatically merges with adjacent NeonBlock instances
/// sharing the same outline color.
///
/// Each side of the block is treated independently and can render as
/// MULTIPLE segments: only the portion of a side that's actually covered by
/// a same-group neighbor is hidden, so a wide/tall block only touching a
/// small neighbor along part of one side still shows the rest of that side.
///
/// The edge children are created automatically at runtime - nothing to wire
/// up in the inspector or the prefab.
///
/// Assumptions: blocks are axis-aligned (no z-rotation) rectangles, and the
/// BoxCollider2D roughly matches the visible sprite bounds.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class NeonBlock : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.cyan;

    [Header("Merging")]
    [Tooltip("ON: only merges with neighbors whose outline color matches exactly (recommended). OFF: merges with any adjacent NeonBlock regardless of color.")]
    [SerializeField] private bool mergeOnlyMatchingOutline = true;

    [Header("Neighbor Detection")]
    [SerializeField] private LayerMask blockLayer = ~0;
    [Tooltip("World-space probe thickness just past each edge.")]
    [SerializeField] private float probeThickness = 0.05f;
    [Tooltip("Minimum overlap length (world units) along a side for a touching block to count as a real neighbor there, rather than a glancing corner touch.")]
    [SerializeField] private float minTouchLength = 0.1f;

    [Tooltip("World-space thickness of the neon line sprites.")]
    [SerializeField] private float lineThickness = 0.08f;

    // One growable pool of segments per side. Segments beyond what's
    // currently needed are just deactivated, not destroyed.
    private readonly List<NeonEdge> segmentsTop = new();
    private readonly List<NeonEdge> segmentsBottom = new();
    private readonly List<NeonEdge> segmentsLeft = new();
    private readonly List<NeonEdge> segmentsRight = new();

    // Reused scratch lists to avoid per-refresh allocations.
    private readonly List<(float start, float end)> _mergeScratch = new();
    private readonly List<(float start, float end)> _exposedScratch = new();
    private readonly HashSet<NeonBlock> _notifyScratch = new();

    private Collider2D _collider;
    private SpriteRenderer _fillRenderer;

    public Color OutlineColor => outlineColor;

    // Shared 1x1 white sprite used by every edge segment. pixelsPerUnit = 1
    // so a localScale of N maps to exactly N world units.
    private static Sprite _pixelSprite;
    private static Sprite PixelSprite
    {
        get
        {
            if (_pixelSprite == null)
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                _pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return _pixelSprite;
        }
    }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _fillRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        RefreshEdges();
        NotifyNeighbors();
    }

    private void OnDisable()
    {
        // Tell neighbors this block is gone so they re-expose any shared edge.
        NotifyNeighbors();
    }

    public void SetOutlineColor(Color c)
    {
        outlineColor = c;
        RefreshEdges();
        NotifyNeighbors();
    }

    /// <summary>Call this if you resize/rescale a block at runtime.</summary>
    public void OnBlockResized()
    {
        RefreshEdges();
        NotifyNeighbors();
    }

    // ---- Per-side geometry helpers ----

    // horizontal sides (top/bottom) vary along x, fixed along y.
    // vertical sides (left/right) vary along y, fixed along x.
    private (float fixedCoord, float varMin, float varMax) GetSideRange(Vector2 dir, bool horizontal)
    {
        Bounds b = _collider.bounds;
        float fixedCoord = horizontal
            ? (dir == Vector2.up ? b.max.y : b.min.y)
            : (dir == Vector2.right ? b.max.x : b.min.x);
        float varMin = horizontal ? b.min.x : b.min.y;
        float varMax = horizontal ? b.max.x : b.max.y;
        return (fixedCoord, varMin, varMax);
    }

    private (Vector2 center, Vector2 size) GetSideProbe(Vector2 dir, bool horizontal, float fixedCoord, float varMin, float varMax)
    {
        if (horizontal)
        {
            var center = new Vector2((varMin + varMax) * 0.5f, fixedCoord + dir.y * probeThickness * 0.5f);
            var size = new Vector2(varMax - varMin, probeThickness);
            return (center, size);
        }
        else
        {
            var center = new Vector2(fixedCoord + dir.x * probeThickness * 0.5f, (varMin + varMax) * 0.5f);
            var size = new Vector2(probeThickness, varMax - varMin);
            return (center, size);
        }
    }

    // ---- Outline refresh: compute exposed sub-intervals per side ----

    public void RefreshEdges()
    {
        RefreshSide(Vector2.up, true, "Top", segmentsTop);
        RefreshSide(Vector2.down, true, "Bottom", segmentsBottom);
        RefreshSide(Vector2.left, false, "Left", segmentsLeft);
        RefreshSide(Vector2.right, false, "Right", segmentsRight);
    }

    private void RefreshSide(Vector2 dir, bool horizontal, string sideName, List<NeonEdge> pool)
    {
        var (fixedCoord, varMin, varMax) = GetSideRange(dir, horizontal);
        var (probeCenter, probeSize) = GetSideProbe(dir, horizontal, fixedCoord, varMin, varMax);

        _mergeScratch.Clear();
        foreach (var hit in Physics2D.OverlapBoxAll(probeCenter, probeSize, 0f, blockLayer))
        {
            if (hit == _collider) continue;
            NeonBlock neighbor = hit.GetComponent<NeonBlock>();
            if (neighbor == null) continue;
            if (mergeOnlyMatchingOutline && neighbor.OutlineColor != outlineColor) continue;

            Bounds nb = hit.bounds;
            float nMin = horizontal ? nb.min.x : nb.min.y;
            float nMax = horizontal ? nb.max.x : nb.max.y;

            float start = Mathf.Max(varMin, nMin);
            float end = Mathf.Min(varMax, nMax);
            if (end - start > minTouchLength)
                _mergeScratch.Add((start, end));
        }

        MergeIntervalsInPlace(_mergeScratch);

        _exposedScratch.Clear();
        float cursor = varMin;
        foreach (var iv in _mergeScratch)
        {
            if (iv.start > cursor)
                _exposedScratch.Add((cursor, iv.start));
            cursor = Mathf.Max(cursor, iv.end);
        }
        if (cursor < varMax)
            _exposedScratch.Add((cursor, varMax));

        ApplySideSegments(_exposedScratch, horizontal, fixedCoord, varMin, varMax, sideName, pool);
    }

    private static void MergeIntervalsInPlace(List<(float start, float end)> intervals)
    {
        if (intervals.Count < 2) return;
        intervals.Sort((a, b) => a.start.CompareTo(b.start));
        int write = 0;
        for (int read = 1; read < intervals.Count; read++)
        {
            if (intervals[read].start <= intervals[write].end + 0.0001f)
                intervals[write] = (intervals[write].start, Mathf.Max(intervals[write].end, intervals[read].end));
            else
                intervals[++write] = intervals[read];
        }
        intervals.RemoveRange(write + 1, intervals.Count - write - 1);
    }

    private void ApplySideSegments(List<(float start, float end)> exposed, bool horizontal, float fixedCoord, float trueMin, float trueMax, string sideName, List<NeonEdge> pool)
    {
        Vector3 lossy = transform.lossyScale;
        float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
        float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;

        while (pool.Count < exposed.Count)
            pool.Add(CreateEdge($"NeonEdgeSeg_{sideName}_{pool.Count}"));

        for (int i = 0; i < pool.Count; i++)
        {
            if (i >= exposed.Count)
            {
                pool[i].gameObject.SetActive(false);
                continue;
            }

            var (start, end) = exposed[i];

            // Only extend outward at the block's true corners. A boundary
            // where this segment meets a same-side different-group neighbor
            // (or a merged region) is a T-junction, not a corner, and should
            // stay flush rather than poke into that neighbor's territory.
            float extStart = Mathf.Approximately(start, trueMin) ? start - lineThickness * 0.5f : start;
            float extEnd = Mathf.Approximately(end, trueMax) ? end + lineThickness * 0.5f : end;

            float center = (extStart + extEnd) * 0.5f;
            float length = extEnd - extStart;

            Vector3 worldPos = horizontal
                ? new Vector3(center, fixedCoord, transform.position.z)
                : new Vector3(fixedCoord, center, transform.position.z);

            var edge = pool[i];
            edge.transform.position = worldPos;

            float localX = horizontal ? length / sx : lineThickness / sx;
            float localY = horizontal ? lineThickness / sy : length / sy;
            edge.transform.localScale = new Vector3(localX, localY, 1f);

            edge.gameObject.SetActive(true);
            edge.SetColor(outlineColor);
        }
    }

    private NeonEdge CreateEdge(string edgeName)
    {
        Transform existing = transform.Find(edgeName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(edgeName);
        go.transform.SetParent(transform, worldPositionStays: false);

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSprite;
        sr.sortingLayerID = _fillRenderer.sortingLayerID;
        sr.sortingOrder = _fillRenderer.sortingOrder + 1; // draw above the fill

        var edge = go.GetComponent<NeonEdge>();
        if (edge == null) edge = go.AddComponent<NeonEdge>();
        return edge;
    }

    // ---- Neighbor notification: every distinct neighbor touching any side ----

    private void NotifyNeighbors()
    {
        _notifyScratch.Clear();
        NotifySide(Vector2.up, true);
        NotifySide(Vector2.down, true);
        NotifySide(Vector2.left, false);
        NotifySide(Vector2.right, false);
    }

    private void NotifySide(Vector2 dir, bool horizontal)
    {
        var (fixedCoord, varMin, varMax) = GetSideRange(dir, horizontal);
        var (probeCenter, probeSize) = GetSideProbe(dir, horizontal, fixedCoord, varMin, varMax);

        foreach (var hit in Physics2D.OverlapBoxAll(probeCenter, probeSize, 0f, blockLayer))
        {
            if (hit == _collider) continue;
            NeonBlock neighbor = hit.GetComponent<NeonBlock>();
            if (neighbor != null && _notifyScratch.Add(neighbor))
                neighbor.RefreshEdges();
        }
    }
}
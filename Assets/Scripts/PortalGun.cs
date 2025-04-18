using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PortalGun : MonoBehaviour
{
    [Header("Aim & Raycast")]
    public float maxDistance = 30f;
    public LayerMask aimLayers = ~0;            // which layers the ray can hit

    [Header("Portal Size Check")]
    public Vector2 portalCheckSize = new Vector2(2f, 2f);
    public float portalCheckOffset = 0.1f; // pull box slightly off the surface
    /// <summary>
    /// What prevents a portal from being placed
    /// </summary>
    public LayerMask blockLayers = ~0;

    [Header("Visuals")]
    public GameObject validIndicatorPrefab;
    public GameObject invalidIndicatorPrefab;
    public LineRenderer lineRenderer;

    private GameObject currentIndicator;

    public Portal currentPortalToSpawn;
    public Portal portalPrefab;
    public Portal[] portals = new Portal[2];
    public int currentPortalIndex = 0;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction  = (mousePos - transform.position).normalized;

        // 1) Raycast toward mouse
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxDistance, aimLayers);

        Vector3 endPoint = hit 
            ? (Vector3)hit.point 
            : (Vector3)((Vector2)transform.position + direction * maxDistance);

        // 2) Draw the line
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, endPoint);
        lineRenderer.colorGradient.colorKeys[0].color = currentPortalToSpawn.color;
        lineRenderer.colorGradient.colorKeys[1].color = currentPortalToSpawn.color;

        // 3) Handle indicator
        if (hit)
        {
            TryPlaceIndicator(hit);
        }
        else
        {
            DestroyIndicator();
        }
    }

    void TryPlaceIndicator(RaycastHit2D hit)
    {
        // Calculate box center slightly off the surface
        Vector2 normal   = hit.normal;
        Vector2 boxCenter = hit.point + normal * portalCheckOffset;

        // Rotate the box so its "width" runs along the surface
        float angleDeg = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;

        // Check for any blockers in that 2×2 area
        Collider2D[] blockers = Physics2D.OverlapBoxAll(
            boxCenter,
            portalCheckSize,
            angleDeg,
            blockLayers
        );

        bool isFree = blockers.Length == 0;

        // Choose prefab
        GameObject prefab = isFree 
            ? validIndicatorPrefab 
            : invalidIndicatorPrefab;

        // Update indicator
        if (currentIndicator != null)
            Destroy(currentIndicator);

        currentIndicator = Instantiate(prefab, hit.point, Quaternion.Euler(0,0,angleDeg));
    }

    void DestroyIndicator()
    {
        if (currentIndicator != null)
            Destroy(currentIndicator);
    }

    // Gizmos to visualize the OverlapBox in the editor
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorld - transform.position).normalized;
        var hit = Physics2D.Raycast(transform.position, dir, maxDistance, aimLayers);
        if (!hit) return;

        Vector2 normal = hit.normal;
        Vector2 center = hit.point + normal * portalCheckOffset;
        float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;

        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0,0,angle), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, portalCheckSize);
    }
}

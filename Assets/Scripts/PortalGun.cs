using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PortalGun : MonoBehaviour
{
    [Header("Aim & Raycast")]
    public float maxDistance = 50f;
    public LayerMask aimLayers = ~0; // which layers the ray can hit

    [Header("Portal Size Check")]
    public Vector2 portalCheckSize = new Vector2(2f, 2f);
    public float portalCheckOffset = 0.1f; // pull box slightly off the surface
    /// <summary>
    /// What prevents a portal from being placed
    /// </summary>
    public LayerMask blockLayers = ~0;

    [Header("Visuals")]
    [SerializeField]
    private GameObject validIndicatorPrefab;
    private GameObject validIndicator;
    [SerializeField]
    private GameObject invalidIndicatorPrefab;
    private GameObject invalidIndicator;
    protected LineRenderer lineRenderer;

    private GameObject currentIndicator;

    protected PortalDescription currentPortalToSpawn;
    public GameObject portalPrefab;
    public List<PortalDescription> portals = new List<PortalDescription>();

    public static PortalController[] portalsInScene;
    private int portalIndex = 0;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
    }

    void Start()
    {
        if (validIndicator == null) validIndicator = Instantiate(validIndicatorPrefab);
        validIndicator.SetActive(false);
        DontDestroyOnLoad(validIndicator);
        if (invalidIndicator == null) invalidIndicator = Instantiate(invalidIndicatorPrefab);
        invalidIndicator.SetActive(false);
        DontDestroyOnLoad(invalidIndicator);
        currentPortalToSpawn = portals[portalIndex];
        portalsInScene = new PortalController[portals.Count];
    }

    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePos - transform.position).normalized;

        // 1) Raycast toward mouse
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxDistance, aimLayers);

        Vector3 endPoint = hit 
            ? (Vector3)hit.point 
            : (Vector3)((Vector2)transform.position + direction.normalized * maxDistance);

        // 2) Draw the line
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, endPoint);
        lineRenderer.startColor = currentPortalToSpawn.color;
        lineRenderer.endColor = currentPortalToSpawn.color;

        // 3) Handle indicator
        RemoveIndicator();
        if (hit)
        {
            Vector2 normal = Vector2.zero;
            if (TryPlaceIndicator(hit, out normal) && Input.GetButtonDown("Fire1"))
            {
                // Spawn the portal
                PortalController portalController = portalsInScene[portalIndex];
                if (portalController != null)
                {
                    portalController.MovePortal(hit.point, normal);
                    // portalController.transform.localScale = new Vector3(2, .1f, 1);
                }
                else
                {
                    GameObject newPortal = Instantiate(portalPrefab, hit.point, Quaternion.identity);
                    portalController = newPortal.GetComponent<PortalController>();
                    portalsInScene[portalIndex] = portalController;
                    portalController.SetupPortal(currentPortalToSpawn,
                        portalIndex, normal);
                }
                GameObject placeholder = new GameObject("PortalPlaceholder");
                placeholder.transform.parent = hit.transform;
                portalController.transform.parent = placeholder.transform;
                portalController.transform.up = hit.normal;
                portalIndex = (portalIndex + 1) % portals.Count;
                currentPortalToSpawn = portals[portalIndex];
                // TODO: Add an array for placeholders
            }
        }
    }

    public void ResetPortals()
    {
        foreach (PortalController portal in portalsInScene)
        {
            if (portal != null)
            {
                Destroy(portal.gameObject);
            }
        }
        portalsInScene = new PortalController[portals.Count];
        portalIndex = 0;
        currentPortalToSpawn = portals[portalIndex];
        validIndicator.SetActive(false);
        invalidIndicator.SetActive(false);
    }

    /// <summary>
    /// Places a visual indicator at the hit point of whether a portal can be placed
    /// </summary>
    /// <param name="hit">Place to check for portal placement</param>
    /// <returns>Whether a portal can be placed there</returns>
    protected bool TryPlaceIndicator(RaycastHit2D hit, out Vector2 normal)
    {
        Vector2 hitPoint = hit.point;
        GameObject indicator;
        normal = hit.normal;
        // Check if we hit a valid surface
        if (hit.transform.CompareTag("Unportalable"))
        {
            // Show the invalid indicator
            indicator = invalidIndicator;
            indicator.SetActive(true);
            indicator.transform.position = hit.point;

            return false;
        }
        Vector2 right = new Vector2(-normal.y, normal.x); // Perpendicular to the normal
        float portalWidth = portalCheckSize.x;
        int steps = Mathf.CeilToInt(portalWidth / 0.5f);
        float stepDistance = portalWidth / steps;

        bool canPlace = true;

        for (int i = -steps/2; i <= steps/2; i++)
        {
            Vector2 offset = right.normalized * stepDistance * i;
            Vector2 testPoint = hitPoint + offset + normal * 0.075f; // slightly behind surface

            // 1. Check side clearance (shoot outward, perpendicular to normal)
            RaycastHit2D sideHit = Physics2D.Raycast(testPoint, offset.normalized, 0.5f, blockLayers);
            if (sideHit)
            {
                canPlace = false;
                break;
            }

            // 2. Check back toward the platform (shoot opposite of normal)
            RaycastHit2D backHit = Physics2D.Raycast(testPoint, -normal, 0.1f, aimLayers);
            if (!backHit)
            {
                canPlace = false;
                break;
            }
        }

        // Show the correct indicator
        indicator = canPlace ? validIndicator : invalidIndicator;
        indicator.SetActive(true);
        indicator.transform.position = hit.point;

        return canPlace;
    }

    /// <summary>
    /// Turns off the indicator of whether a portal can be placed
    /// </summary>
    private void RemoveIndicator()
    {
        validIndicator.SetActive(false);
        invalidIndicator.SetActive(false);
    }

    // Gizmos to visualize the OverlapBox in the editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorld - transform.position).normalized;
        var hit = Physics2D.Raycast(transform.position, dir, maxDistance, aimLayers);
        if (!hit) return;

        Vector2 hitPoint = hit.point;
        Vector2 normal = hit.normal;
        Vector2 right = new Vector2(-normal.y, normal.x); // perpendicular to normal
        float portalWidth = portalCheckSize.x;
        int steps = Mathf.CeilToInt(portalWidth / 0.5f);
        float stepDistance = portalWidth / steps;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(hitPoint, 0.05f);

        for (int i = -steps/2; i <= steps/2; i++)
        {
            Vector2 offset = right.normalized * stepDistance * i;
            Vector2 testPoint = hitPoint + offset + normal * .1f;

            // Draw back check ray (toward surface
            Gizmos.color = Color.green;
            Gizmos.DrawLine(testPoint, testPoint - normal * 0.1f);

            // Draw test point
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(testPoint, 0.05f);

            // Draw side check ray (outward)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(testPoint, testPoint + offset.normalized * 0.5f);

            // Draw back check ray (toward surface)
            Gizmos.color = Color.green;
            Gizmos.DrawLine((testPoint + offset.normalized * 0.5f), (testPoint + offset.normalized * 0.5f) - normal * 0.1f);
        }
    }
}

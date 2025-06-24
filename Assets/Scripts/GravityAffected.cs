using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GravityAffected : MonoBehaviour
{
    [HideInInspector]
    public Rigidbody2D rb;

    public bool autoRespawning;
    public Vector2 respawnPosition;

    public PolygonCollider2D cameraBounds;
    
    public float gravityAcceleration = 9.8f;
    public float terminalVelocity = 20f; // max falling speed
    [HideInInspector]
    public Vector2 gravityDirection = Vector2.down;
    public Vector2 defaultGravityDirection = Vector2.down;

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        respawnPosition = transform.position;
        gravityDirection = defaultGravityDirection.normalized;
        GameObject cameraBoundsGO = GameObject.Find("Camera Bounds");
        Debug.Assert(cameraBoundsGO != null, "Camera Bounds couldn't be found with name Camera Bounds");
        cameraBounds = cameraBoundsGO.GetComponent<PolygonCollider2D>();
    }

    protected virtual void FixedUpdate()
    {
        Vector2 gravDir = gravityDirection.normalized;

        // Step 1: Remove vertical component of drag
        Vector2 velocity = rb.velocity;
        float dragFactor = 1f / (1f + rb.drag * Time.fixedDeltaTime);

        float velInGravDir = Vector2.Dot(velocity, gravDir);
        float velOrthogonal = velocity.magnitude * Mathf.Sqrt(1 - Mathf.Pow(Vector2.Dot(velocity.normalized, gravDir), 2));

        Vector2 vGrav     = gravDir * velInGravDir;
        Vector2 vSideways = velocity - vGrav;

        Vector2 correctedVGrav = vGrav / dragFactor;
        rb.velocity = correctedVGrav + vSideways;

        // Step 2: Manually apply gravity until terminal velocity
        float newVelInGravDir = Vector2.Dot(rb.velocity, gravDir);
        if (newVelInGravDir < terminalVelocity)
        {
            float deltaV = gravityAcceleration * Time.fixedDeltaTime;
            float cappedDelta = Mathf.Min(deltaV, terminalVelocity - newVelInGravDir);
            rb.velocity += gravDir * cappedDelta;
        }

        // Step 3: Step-up logic 
        // TryStepUp(vSideways); // TODO: Reimplement and uncomment
    }

    protected virtual void Update() {

        if (Vector3.Distance(cameraBounds.ClosestPoint(transform.position), transform.position) > 10 && autoRespawning)
        {
            Reset();
        }
    }

    public void Reset()
    {
        gravityDirection = defaultGravityDirection;
        transform.position = respawnPosition;
        rb.velocity = Vector2.zero;
    }


    [SerializeField] float stepHeight = 0.2f;
    [SerializeField] float stepCheckDistance = 0.1f;
    [SerializeField] float height = 1f;
    [SerializeField] LayerMask stepLayerMask;

    private void TryStepUp(Vector2 velocity)
    {
        if (velocity == Vector2.zero) return;

        Vector2 gravDir = gravityDirection.normalized;
        Vector2 moveDir = velocity.normalized;

        // Step 1: Raycast straight down to see if we're on the ground
        RaycastHit2D groundHit = Physics2D.Raycast(transform.position, gravDir, height/2, stepLayerMask);
        if (!groundHit.collider) return; // Not grounded

        // Step 2: From that hit point, raycast in move direction to detect obstacle
        Vector2 from = groundHit.point + moveDir * stepCheckDistance;
        RaycastHit2D forwardHit = Physics2D.Raycast(from, gravDir, stepHeight, stepLayerMask);

        Debug.DrawRay(transform.position, gravDir * 0.1f, Color.yellow);           // Ground check
        Debug.DrawRay(from, gravDir * stepHeight, Color.red);                     // Step check

        if (forwardHit.collider == null)
        {
            // No blocker above the small step — allow stepping up
            rb.position += -gravDir * stepHeight;
        }
    }
}

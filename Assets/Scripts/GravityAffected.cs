using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GravityAffected : MonoBehaviour
{
    public Rigidbody2D rb;
    
    public float gravityAcceleration = 9.8f;
    public float terminalVelocity = 20f; // max falling speed
    public Vector2 gravityDirection = Vector2.down;

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    protected virtual void FixedUpdate()
    {
        Vector2 gravDir = gravityDirection.normalized;

        // Step 1: Remove vertical component of drag
        Vector2 velocity = rb.velocity;

        float dragFactor = 1f / (1f + rb.drag * Time.fixedDeltaTime);

        float velInGravDir = Vector2.Dot(velocity, gravDir);
        float velOrthogonal = velocity.magnitude * Mathf.Sqrt(1 - Mathf.Pow(Vector2.Dot(velocity.normalized, gravDir), 2));

        // Reconstruct horizontal and vertical velocity separately
        Vector2 vGrav     = gravDir * velInGravDir;
        Vector2 vSideways = velocity - vGrav;

        // Reverse Unity's drag on vertical part
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
    }
}

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpecialGravityZone : GravityZone
{
    public float gravityAccelerationMultiplier = 0.5f; // Multiplier for gravity acceleration in this zone
    private void OnTriggerEnter2D(Collider2D other)
    {
        GravityAffected gravity = other.GetComponent<GravityAffected>();
        if (gravity != null)
        {
            gravity.gravityDirection = gravityDirection.normalized;
            gravity.gravityAcceleration *= gravityAccelerationMultiplier;
            gravity.terminalVelocity *= gravityAccelerationMultiplier;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GravityAffected gravity = other.GetComponent<GravityAffected>();
        if (gravity != null)
        {
            gravity.gravityDirection = gravity.defaultGravityDirection;
            gravity.gravityAcceleration /= gravityAccelerationMultiplier;
            gravity.terminalVelocity /= gravityAccelerationMultiplier;
        }
    }
}

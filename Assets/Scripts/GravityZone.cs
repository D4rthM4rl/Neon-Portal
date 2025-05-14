using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityZone : MonoBehaviour
{
    public Vector2 gravityDirection = Vector2.left; // Local gravity for this zone

    private void OnTriggerEnter2D(Collider2D other)
    {
        GravityAffected gravity = other.GetComponent<GravityAffected>();
        if (gravity != null)
        {
            gravity.gravityDirection = gravityDirection.normalized;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GravityAffected gravity = other.GetComponent<GravityAffected>();
        if (gravity != null)
        {
            gravity.gravityDirection = gravity.defaultGravityDirection; // Optional: return to default gravity
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// What type of portal is this, does it have special properties or just put you
/// through the next portal?
/// </summary>
public enum PortalType
{
    Default,
    GravitySwitching,
}

[System.Serializable]
public struct PortalDescription
{
    public Color color;
    public PortalType type;
}

[System.Serializable]
[RequireComponent(typeof(PolygonCollider2D))]
public class PortalController : MonoBehaviour
{
    /// <summary>
    /// What actually gets spawned when we place a portal
    /// </summary>
    private PortalDescription description;
    private PortalController receivingPortal;
    public Vector2 direction; 

    private int index;

    public void SetupPortal (PortalDescription description, int index, Vector2 direction)
    {
        this.description = description;
        gameObject.GetComponent<SpriteRenderer>().color = description.color;
        this.index = index;
        this.direction = direction;
        GetComponent<Light2D>().color = description.color;
    }
    
    private void OnTriggerEnter2D(Collider2D other) {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (other.gameObject != null && rb != null)
        {
            if (PortalGun.portalsInScene[(index + 1) % PortalGun.portalsInScene.Length] != null)
            {
                Vector2 incomingVelocity = rb.velocity;
                Debug.Log("Incoming Velocity: " + incomingVelocity);

                // Correct SignedAngle usage
                float angleDifference = Vector3.SignedAngle(transform.up, receivingPortal.transform.up, Vector3.forward);

                // Rotate the incoming velocity by that angle
                Vector2 rotatedVelocity = Quaternion.Euler(0, 0, angleDifference) * incomingVelocity;

                // Teleport and set new velocity
                other.transform.position = receivingPortal.transform.position + (Vector3)receivingPortal.direction * 1f;
                rb.velocity = -rotatedVelocity;
                Debug.Log("New Velocity: " + rb.velocity);
            }
        }
    }

    public void MovePortal(Vector2 newPos, Vector2 newDirection)
    {
        transform.position = newPos;
        direction = newDirection;
    }
}

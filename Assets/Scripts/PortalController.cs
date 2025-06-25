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

    public int index;

    /// <summary>
    /// Initializes the portal from its description
    /// </summary>
    /// <param name="description">PortalDescription to create from</param>
    /// <param name="index">What its index is, to see what it connects to</param>
    /// <param name="direction">What direction its facing</param>
    public void SetupPortal (PortalDescription description, int index, Vector2 direction)
    {
        this.description = description;
        this.index = index;
        this.direction = direction;
        Color color;
        if (index == 0)
        {
            if (Settings.instance != null) color = Settings.instance.portal1Color;
            else color = Color.magenta;
        }
        else if (index == 1)
        {
            if (Settings.instance != null) color = Settings.instance.portal2Color;
            else color = Color.yellow;
        }
        else
        {
            Debug.LogError("Invalid portal index: " + index);
            color = Color.white; // Fallback color
        }
        GetComponent<Light2D>().color = color;
        Debug.Assert(GetComponentInChildren<SpriteRenderer>() != null, "No sprite renderer found in children of portal");
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.color = color;
        }
    }

    public bool IsConnected()
    {
        PortalController receivingPortal = PortalGun.portalsInScene[(index + 1) % PortalGun.portalsInScene.Length];
        return receivingPortal != null;
    }
    
    private void OnTriggerEnter2D(Collider2D other) {
        Teleportable tpObj = other.GetComponent<Teleportable>();
        if (other.gameObject == null || !tpObj) return;


        PortalController receivingPortal = PortalGun.portalsInScene[(index + 1) % PortalGun.portalsInScene.Length];
        if (receivingPortal != null)
        {
            Player player = other.GetComponent<Player>();
            if ((Settings.instance == null || Settings.instance.needToTouchGroundToReenterPortal)
                && player && player.cantReenterIndex == index) return;
            else if (player) player.cantReenterIndex = receivingPortal.index;

            Vector2 incomingVelocity = GetVelocity(tpObj);
            // Debug.Log("Incoming Velocity: " + incomingVelocity);

            float angleDifference = Vector3.SignedAngle(transform.up, receivingPortal.transform.up, Vector3.forward);

            // Rotate the incoming velocity by that angle
            Vector2 rotatedVelocity = Quaternion.Euler(0, 0, angleDifference) * incomingVelocity;
            if (rotatedVelocity.magnitude < 1) rotatedVelocity = rotatedVelocity.normalized;
            float separation = 1;
            if (other.GetComponent<Player>() == null)
            {
                if (Mathf.Abs(receivingPortal.direction.x) >= Mathf.Abs(receivingPortal.direction.y))
                    separation = other.transform.localScale.x / 2;
                else
                    separation = other.transform.localScale.y / 2;
                separation += .1f;
            }

            // Teleport and set new velocity
            other.transform.position = receivingPortal.transform.position + 
                (Vector3)receivingPortal.direction * separation;
            tpObj.rb.velocity = -rotatedVelocity;
            if (receivingPortal.description.type == PortalType.GravitySwitching)
            {
                tpObj.gravityDirection = -receivingPortal.direction;
            }
            // Debug.Log("New Velocity: " + rb.velocity);
        }
    }

    private Vector2 GetVelocity(Teleportable teleportableObject)
    {
        return teleportableObject.previousVelocities[1];
        // foreach (Vector2 v in teleportableObject.previousVelocities)
        // {
        //     if (v != Vector2.zero)
        //     {
        //         return v;
        //     }
        // }
        // return Vector2.zero;
    }

    public void MovePortal(Vector2 newPos, Vector2 newDirection)
    {
        transform.position = newPos;
        direction = newDirection;
    }
}

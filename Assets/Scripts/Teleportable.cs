using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleportable : GravityAffected
{
    public Vector2[] previousVelocities = new Vector2[3];

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        // Store the current velocity in the array
        for (int i = previousVelocities.Length - 1; i > 0; i--)
        {
            previousVelocities[i] = previousVelocities[i - 1];
        }
        previousVelocities[0] = rb.velocity;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingBlock : MonoBehaviour
{
    public List<Vector2> positions = new List<Vector2>();
    [SerializeField]
    private float speed = 2.0f;
    [SerializeField]
    private float waitAtEachPosition = 1.0f;
    [SerializeField]
    [Tooltip("If true, will move toward the first position after the last one. If false, will reverse through the positions.")]
    private bool loop = true;

    private int currentIndex = 0;
    private bool waiting = false;
    private float waitTimer = 0.0f;

    private void Start() {
        positions.Insert(0, transform.position); // Ensure the first position is the current position
    }

    // Update is called once per frame
    void Update()
    {
        if (positions.Count == 0)
            return;

        if (waiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitAtEachPosition)
            {
                waiting = false;
                waitTimer = 0.0f;
                currentIndex = loop ? (currentIndex + 1) % positions.Count : Mathf.Clamp(currentIndex + 1, 0, positions.Count - 1);
            }
            return;
        }

        Vector3 targetPosition = positions[currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            waiting = true;
        }
    }

    public void Reset()
    {
        transform.position = positions[0];
        currentIndex = 0;
        waiting = false;
        waitTimer = 0;
    }
}

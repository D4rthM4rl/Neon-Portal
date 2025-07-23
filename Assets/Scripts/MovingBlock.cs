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
    [Tooltip("Move toward the first position after the last one, reverse through the positions, or stop at the last position")]
    private LoopType loopType = LoopType.Loop;
    [SerializeField]
    private bool waitForProximity = false;

    private int currentIndex = 0;
    private bool waiting = false;
    private bool waitingForProximity = false;
    private float waitTimer = 0.0f;

    private GameObject player;

    private void Start() {
        positions.Insert(0, transform.position); // Ensure the first position is the current position
        waitingForProximity = waitForProximity;

        GameObject[] playerTags = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in playerTags)
        {
            if (p.GetComponent<Player>() != null)
            {
                player = p;
                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (positions.Count == 0)
            return;

        if (waitForProximity && waitingForProximity)
        {
            if (Vector3.Distance(transform.position, player.transform.position) < 5)
            {
                waitingForProximity = false;
                waiting = true;
                waitTimer = 0.0f;
            }
            return;
        }

        if (waiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitAtEachPosition)
            {
                if (currentIndex >= positions.Count - 1)
                {
                    if (loopType == LoopType.Stop) return; // Stop moving if at the last position
                    else if (loopType == LoopType.Restart)
                    {
                        Reset();
                        return;
                    }
                }
                waiting = false;
                waitTimer = 0.0f;
                currentIndex = loopType == LoopType.Loop ? (currentIndex + 1) % positions.Count :
                   Mathf.Clamp(currentIndex + 1, 0, positions.Count - 1);
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
        waitingForProximity = waitForProximity;
        waiting = false;
        waitTimer = 0;
    }
}

public enum LoopType
{
    Loop,
    Reverse,
    Stop,
    Restart,
}

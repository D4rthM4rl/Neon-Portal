using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProximity : MonoBehaviour
{
    public float minimumTransparency = 0.35f;
    public float decreasePerUnit = 0.1f;

    public float radius = 3;
    private GameObject player;
    public List<Vector2> targetPositions = new List<Vector2>();
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (targetPositions.Count == 0)
        {
            targetPositions.Add(transform.position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 closestPos = targetPositions[0];
        foreach (Vector2 pos in targetPositions)
        {
            if (Vector2.Distance(player.transform.position, pos) < Vector2.Distance(player.transform.position, closestPos))
            {
                closestPos = pos;
            }
        }
        float distance = Vector2.Distance(player.transform.position, closestPos);
        float transparency = Mathf.Clamp(1 - Mathf.Max(distance - radius, 0) * decreasePerUnit, minimumTransparency, 1);
        Color color = GetComponent<SpriteRenderer>().color;
        color.a = transparency;
        GetComponent<SpriteRenderer>().color = color;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    /// <summary>
    /// What actually gets spawned when we place a portal
    /// </summary>
    public GameObject portalPrefab;

    public Color color = Color.blue;

    void Awake()
    {
        // Set the color of the portal
        GetComponent<SpriteRenderer>().color = color;
    }
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

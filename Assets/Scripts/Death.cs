using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Death : MonoBehaviour
{   
    private void OnCollisionEnter2D(Collision2D other) {
        if (other.gameObject.CompareTag("Player")) {
            // Assuming the player has a method to handle death
            Player playerController = other.gameObject.GetComponent<Player>();
            if (playerController != null) {
                playerController.ResetPlayer();
                playerController.ResetWorld();
                playerController.ResetPortals();
            }
        }
    }
}

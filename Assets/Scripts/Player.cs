using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool isGrounded = true;

    public float  initialJumpForce = 4f;    // impulse on button down
    public float  extraJumpForce   = 7f;    // continuous “hold” force
    public float  maxJumpDuration  = 0.3f;  // how long you can hold
    public float  moveSpeed        = 20f;   // your horizontal speed
    
    private bool      isJumping;           // are we in the “hold” phase?
    private float     jumpTimeCounter;     // how much “hold time” left
    public float groundCheckDistance = .58f;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // If escape is pressed, pause the game by stopping time
        if (Input.GetButtonDown("Pause"))
        {
            PauseMenuController.instance.ToggleMenu();
        }
        
        // isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, LayerMask.GetMask("Ground"));
        if (Input.GetButton("Jump"))
        {
            Jump();
        }
        else
        {
            isJumping = false;
            jumpTimeCounter = maxJumpDuration;
        }

        if (transform.position.y < -10f)
        {
            // Reset the player position if they fall off the screen
            transform.position = new Vector3(0, 0, 0);
            rb.velocity = Vector2.zero; // Reset velocity
        }
    }

    void FixedUpdate() 
    {
        // isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, LayerMask.GetMask("Ground"));
        
        // 4) Horizontal movement 
        float h = Input.GetAxisRaw("Horizontal");
        Vector2 hVel = Vector2.right * h * Time.deltaTime;
        rb.AddForce(hVel * 100, ForceMode2D.Impulse);
    }

    void Jump()
    {
        if (!isJumping && isGrounded) 
        {
            isJumping = true;
            jumpTimeCounter = maxJumpDuration;
            rb.AddForce(initialJumpForce * Vector2.up, ForceMode2D.Impulse);
            Debug.Log("JUMP START");
        }
        else
        {
            if (jumpTimeCounter > 0f && isJumping)
            {
                // apply small extra lift each frame
                rb.AddForce(extraJumpForce * Vector2.up * Time.fixedDeltaTime, ForceMode2D.Impulse);
                
                jumpTimeCounter -= Time.fixedDeltaTime;
            }
            else
            {
                // Ran out of “hold” time
                isJumping = false;
                if (isGrounded)
                    jumpTimeCounter = maxJumpDuration;
            }
        }
    }

    

  // You need to flip this on when you land:
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            Debug.Log("Landed");
        }
    }

    void OnCollisionExit2D(Collision2D col) {
        if (col.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            Debug.Log("Left Ground");
        }
    }

}

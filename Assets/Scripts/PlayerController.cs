using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Add the required components if they don't exist
        if (GetComponent<PlayerInventory>() == null)
            gameObject.AddComponent<PlayerInventory>();
        
        if (GetComponent<PlayerInteraction>() == null)
            gameObject.AddComponent<PlayerInteraction>();
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void HandleInput()
    {
        movement.x = 0f;
        movement.y = 0f;

        // WASD Controls
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            movement.y = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            movement.y = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            movement.x = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            movement.x = 1f;

        // Normalize diagonal movement
        movement = movement.normalized;
    }

    void MovePlayer()
    {
        rb.linearVelocity = movement * moveSpeed;
    }
}

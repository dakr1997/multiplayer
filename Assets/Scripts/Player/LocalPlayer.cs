using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;


public class LocalPlayer : NetworkBehaviour
{

    // ###### VARIABLES ######

    // Movement variables
    public float moveSpeed = 5f;
    
    // Shooting variables
    //public GameObject projectilePrefab;
    //public Transform shootingPoint;
    public float playerDamage = 10f;
    public float shootCooldown = 0.5f;
    private float lastShootTime;

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerHealth healthComponent;

    // Networking
    //public NetworkVariable<Vector2> Position = new NetworkVariable<Vector2>();

    // Animation tracking
    private string currentAnimation = "";



    // ###### Base Functions ######



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   // Initialize components and variables

        rb = GetComponent<Rigidbody2D>(); 
        // Mitigation against Shaking
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; 

        // Animator Initialization
        animator = GetComponent<Animator>();

        // Health Initialization
        healthComponent = GetComponent<PlayerHealth>();

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return; // Only the owner can control the player
        // Player Movement and Animation
        Move();
    }



    // ##### Custom Functions #####
    public void Move()
    {
        float moveX = Input.GetAxis("Horizontal") * moveSpeed;
        float moveY = Input.GetAxis("Vertical") * moveSpeed;

        Vector2 movement = new Vector2(moveX, moveY);
        rb.linearVelocity = movement;
        // Simplified animation switching
        if (movement.magnitude > 0.1f)
        {
            ChangeAnimation(Mathf.Abs(moveX) > Mathf.Abs(moveY) 
                ? moveX > 0 ? "walk_right" : "walk_left" 
                : moveY > 0 ? "walk_up" : "walk_down");
        }
        else
        {
            ChangeAnimation("idle");
        }
    }

    public void TakeDamage(float damage, string source)
    {
        healthComponent?.TakeDamage((int)damage);
        Debug.Log($"Player took {damage} from {source}!");
    }

    // Function to shoot a projectile **TODO**

    public void ChangeAnimation(string animation, float crossfade = 0.1f)
    {
        if (currentAnimation != animation)
        {
            if (animation == "idle")
            {
                animation = currentAnimation switch
                {
                    _ when currentAnimation.EndsWith("_right") => "idle_right",
                    _ when currentAnimation.EndsWith("_left") => "idle_left",
                    _ when currentAnimation.EndsWith("_up") => "idle_up",
                    _ when currentAnimation.EndsWith("_down") => "idle_down",
                    _ => animation
                };
            }

            currentAnimation = animation;
            animator.CrossFade(animation, crossfade);
        }
    }

    public void Shoot()
    {
        Debug.Log("Shooting");
    }


    private void Die()
    {
        Debug.Log("Player died!");
    }


    private void TestDamage()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(10f, "TestDamage");
        }
    }


}

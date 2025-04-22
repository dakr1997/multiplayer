using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;


public class LocalPlayer : NetworkBehaviour
{

    // ###### VARIABLES ######

    // HUD variables
    public GameObject HUDCanvasPrefab;
    private GameObject HUDInstance;
    // Movement variables
    public float moveSpeed = 5f;
    
    private string PlayerName = "Player"; // Default name for the player
    // Shooting variables
    //public GameObject projectilePrefab;
    //public Transform shootingPoint;
    public float playerDamage = 10f;
    public float shootCooldown = 0.5f;
    private float lastShootTime;
    
    // Components
    private Rigidbody2D rb;
    private TowerHealth towerHealth;
    private Animator animator;
    private PlayerHealth healthComponent;
    private PlayerExperience experienceComponent;
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

        if (IsOwner && PlayerCameraFollow_Smooth.Instance != null)
        {
            PlayerCameraFollow_Smooth.Instance.SetTarget(transform);
            SpawnHUD(); // Spawn HUD only for the owner
            InitializeHealthComponent();
            InitializeExperienceComponent(); // Initialize experience component
            InitializeTowerHealthComponent(); // <- New line here
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return; // Only the owner can control the player
        // Player Movement and Animation
        Move();
        TestDamage(); // Test damage function for debugging
        TestExperience(); // Test experience function for debugging
    }



    // ##### Custom Functions #####


    void SpawnHUD()
    {
        if (HUDCanvasPrefab != null)
        {

            Canvas canvas = FindObjectOfType<Canvas>();

            HUDInstance = Instantiate(HUDCanvasPrefab, canvas.transform);
        

        }
        else
        {
            Debug.LogWarning("HUD Prefab is not assigned!");
        }

    }

    private void InitializeHealthComponent()
    {
        healthComponent = GetComponent<PlayerHealth>();
        if (healthComponent != null)
        {
            healthComponent.SetHUDReference(HUDInstance); // New method we'll add
        }
    }

    private void InitializeTowerHealthComponent()
    {
        // Optional: assign the tower via inspector if there's only one
        towerHealth = GameObject.FindGameObjectWithTag("Tower")?.GetComponent<TowerHealth>();

        if (towerHealth != null)
        {
            towerHealth.OnHealthChanged += UpdateTowerHealthHUD;
        }
        else
        {
            Debug.LogWarning("TowerHealth component not found! Check Tower GameObject has correct tag and component.");
        }

        if (towerHealth != null)
        {
            towerHealth.OnHealthChanged += UpdateTowerHealthHUD;
        }
        else
        {
            Debug.LogWarning("TowerHealth component not found!");
        }
    }

    private void UpdateTowerHealthHUD(float current, float max)
    {
        if (HUDInstance == null) return;

        Slider towerSlider = HUDInstance.transform.Find("HealthBarTower_main")?.GetComponent<Slider>();
        if (towerSlider != null)
        {
            towerSlider.maxValue = max;
            towerSlider.value = current;
        }
        else
        {
            Debug.LogWarning("TowerHealthBar not found in HUD!");
        }
    }


    private void InitializeExperienceComponent()
    {
        experienceComponent = GetComponent<PlayerExperience>();
        if (experienceComponent != null)
        {
            experienceComponent.SetHUDReference(HUDInstance); // New method we'll add
        }
    }


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

private void TestExperience()
{
    if (Input.GetKeyDown(KeyCode.E))
    {
        experienceComponent?.GainEXPServerRpc(30f); // Gain 30 EXP on E key
    }
}
}

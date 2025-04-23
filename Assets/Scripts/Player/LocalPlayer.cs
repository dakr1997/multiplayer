using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class LocalPlayer : NetworkBehaviour
{
    [Header("HUD Settings")]
    public GameObject HUDCanvasPrefab;
    
    [Header("Debug Settings")]
    public KeyCode debugDamageKey = KeyCode.Space;
    public KeyCode debugExpKey = KeyCode.E;
    public float debugDamageAmount = 10f;
    public float debugExpAmount = 30f;

    // Component References
    private GameObject HUDInstance;
    private PlayerHUDController hudController;
    private Rigidbody2D rb;
    private TowerHealth towerHealth;

    [Header("Player Components")]
    private PlayerHealth healthComponent;
    private PlayerExperience experienceComponent;
    private PlayerMovement playerMovement;

    void Start()
    {
        InitializeComponents();
        
        if (IsOwner && PlayerCameraFollow_Smooth.Instance != null)
        {
            PlayerCameraFollow_Smooth.Instance.SetTarget(transform);
            InitializeHUD();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleDebugInput();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        healthComponent = GetComponent<PlayerHealth>();
        experienceComponent = GetComponent<PlayerExperience>();
        playerMovement = GetComponent<PlayerMovement>();
        
        FindTowerHealth();
    }

    private void InitializeHUD()
    {
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        GameObject hudInstance;
        
        if (mainCanvas != null)
        {
            hudInstance = Instantiate(HUDCanvasPrefab, mainCanvas.transform);
        }
        else
        {
            hudInstance = Instantiate(HUDCanvasPrefab);
            Debug.LogWarning("No main Canvas found in scene - creating standalone HUD");
        }

        hudController = hudInstance.GetComponent<PlayerHUDController>();
        hudController.Initialize(
            GetComponent<PlayerHealth>(),
            GetComponent<PlayerExperience>(),
            FindObjectOfType<TowerHealth>()
        );
    }

    private void FindTowerHealth()
    {
        GameObject tower = GameObject.FindGameObjectWithTag("Tower");
        if (tower != null)
        {
            towerHealth = tower.GetComponent<TowerHealth>();
            if (towerHealth == null)
            {
                Debug.LogWarning("Tower found but missing TowerHealth component");
            }
        }
        else
        {
            Debug.LogWarning("No GameObject with 'Tower' tag found in scene");
        }
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(debugDamageKey))
        {
            TakeDamage(debugDamageAmount, "DebugDamage");
        }

        if (Input.GetKeyDown(debugExpKey))
        {
            // Updated to use the new XPManager system
            if (XPManager.Instance != null)
            {
                XPManager.Instance.AwardXP(NetworkManager.LocalClientId, (int)debugExpAmount);
            }
            else
            {
                Debug.LogError("XPManager instance not found!");
            }
        }
    }

    public void TakeDamage(float damage, string source)
    {
        if (!IsOwner) return;
        
        healthComponent?.TakeDamage((int)damage);
        Debug.Log($"{gameObject.name} took {damage} damage from {source}");
    }

    private void OnDestroy()
    {
        if (towerHealth != null && hudController != null)
        {
            towerHealth.OnHealthChanged -= hudController.UpdateTowerHealth;
        }
    }
}
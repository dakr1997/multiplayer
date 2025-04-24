using UnityEngine;
using Unity.Netcode;

public class PlayerClientHandler : MonoBehaviour
{
    [Header("HUD Settings")]
    public GameObject HUDCanvasPrefab;

    [Header("Debug Settings")]
    public KeyCode debugDamageKey = KeyCode.Space;
    public KeyCode debugExpKey = KeyCode.E;
    public float debugDamageAmount = 10f;
    public float debugExpAmount = 30f;

    private GameObject hudInstance;
    private PlayerHUDController hudController;
    private TowerHealth towerHealth;

    private PlayerEntity player;

    public void Initialize(PlayerEntity entity)
    {
        player = entity;

        SetupCamera();
        SetupHUD();
        FindTowerHealth();
    }

    private void Update()
    {
        if (!player.IsOwner) return;
        HandleDebugInput();
    }

    private void SetupCamera()
    {
        if (PlayerCameraFollow_Smooth.Instance != null)
        {
            PlayerCameraFollow_Smooth.Instance.SetTarget(player.transform);
        }
    }

    private void SetupHUD()
    {
        Canvas mainCanvas = FindObjectOfType<Canvas>();

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
            player.HealthComponent,
            player.ExperienceComponent,
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
            player.HealthComponent?.TakeDamage((int)debugDamageAmount);
        }

        if (Input.GetKeyDown(debugExpKey))
        {
            if (XPManager.Instance != null)
            {
                XPManager.Instance.AwardXPToAll((int)debugExpAmount);
            }
            else
            {
                Debug.LogError("XPManager instance not found!");
            }
        }
    }

    private void OnDestroy()
    {
        if (towerHealth != null && hudController != null)
        {
            towerHealth.OnHealthChanged -= hudController.UpdateTowerHealth;
        }
    }
}

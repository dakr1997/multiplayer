using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
public class PlayerClientHandler : NetworkBehaviour
{
    private GameObject hudInstance;
    private PlayerHUDController hudController;

    public GameObject HUDCanvasPrefab; // Assign this in the inspector
    private PlayerEntity player;

    public void Initialize(PlayerEntity entity)
    {
        player = entity;
        SetupCamera();
        SetupHUD();
    }

    private void Update()
    {
        if (!IsOwner) return; // Only the local player should handle input

        if (Input.GetKeyDown(KeyCode.X)) // Press "X" to test XP gain
        {
            Debug.Log($"[XP TEST] Player {OwnerClientId} requesting 50 XP");

            if (XPManager.Instance != null)
            {
                XPManager.Instance.AwardXPToAll(50); // Give 50 XP to self
            }
            else
            {
                Debug.LogError("[XP TEST] XPManager instance not found!");
            }
        }
        if (Input.GetKeyDown(KeyCode.H)) // Press "H" to test damage
        {
            var mainTower = MainTowerHP.Instance; // Or find it another way
            
            if (mainTower != null)
            {
                Debug.Log($"[HP TEST] Player {OwnerClientId} requesting 50 HP");
                mainTower.RequestSetHPServerRpc(50);
            }
            else
            {
                Debug.LogError("[HP TEST] Main tower not found!");
            }
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
            MainTowerHP.Instance // Assuming MainTowerHP is a singleton or accessible instance
        );
    }


    private void SetupCamera()
    {
        if (PlayerCameraFollow_Smooth.Instance != null)
        {
            PlayerCameraFollow_Smooth.Instance.SetTarget(player.transform);
        }
    }

 
}

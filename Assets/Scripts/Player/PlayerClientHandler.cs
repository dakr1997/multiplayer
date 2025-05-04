using UnityEngine;
using System;
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
        // Look specifically for the HUD_Canvas instead of any Canvas
        Canvas hudCanvas = GameObject.Find("HUD_Canvas")?.GetComponent<Canvas>();
        
        if (hudCanvas != null)
        {
            hudInstance = Instantiate(HUDCanvasPrefab, hudCanvas.transform);
            Debug.Log("HUD spawned on HUD_Canvas");
        }
        else
        {
            Debug.LogError("HUD_Canvas not found in scene! Falling back to any available Canvas.");
            
            // Fallback to any Canvas
            Canvas anyCanvas = FindObjectOfType<Canvas>();
            if (anyCanvas != null)
            {
                hudInstance = Instantiate(HUDCanvasPrefab, anyCanvas.transform);
                Debug.LogWarning("HUD spawned on " + anyCanvas.name + " as fallback");
            }
            else
            {
                // Last resort - create standalone
                hudInstance = Instantiate(HUDCanvasPrefab);
                Debug.LogWarning("No Canvas found in scene - creating standalone HUD");
            }
        }

        hudController = hudInstance.GetComponent<PlayerHUDController>();
        hudController.Initialize(
            player.Health,
            player.Experience,
            MainTowerHP.Instance
        );
    }


    private void SetupCamera()
    {
        if (PlayerCameraFollow_Smooth.Instance != null)
        {
            PlayerCameraFollow_Smooth.Instance.SetTarget(player.transform);
        }
    }

    private void OnDestroy()
    {
        if (hudController != null && player != null)
        {
            // Unsubscribe from events
            if (player.Health != null)
            {
                player.Health.OnHealthChanged -= hudController.UpdateHealth;
            }
            
            if (player.Experience != null)
            {
                // Create a proper method reference for event handling
                var expHandler = new Action<float, float, int>((current, max, level) => {
                    hudController.UpdateExp(current, max);
                    hudController.UpdateLevel(level);
                });
                
                player.Experience.OnExpChanged -= expHandler;
            }
        }
    }

 
}

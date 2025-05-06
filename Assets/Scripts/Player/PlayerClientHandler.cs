using UnityEngine;
using System;
using Unity.Netcode;
using System.Collections;
using Core.Towers.MainTower;
using Player.Base;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerClientHandler : NetworkBehaviour
{
    private GameObject hudInstance;
    private PlayerHUDController hudController;

    public GameObject HUDCanvasPrefab; // Assign this in the inspector
    private PlayerEntity player;
    
    // Add delay configuration
    [SerializeField] private float initialDelay = 1.0f;
    [SerializeField] private float retryInterval = 0.5f;
    [SerializeField] private int maxRetries = 50;
    
    private bool hasInitialized = false;

    public void Initialize(PlayerEntity entity)
    {
        player = entity;
        
        // Only set up once
        if (!hasInitialized)
        {
            hasInitialized = true;
            
            // Start checking if we're in the game scene
            StartCoroutine(WaitForGameScene());
        }
    }
    
    private IEnumerator WaitForGameScene()
    {
        // Wait for initial delay
        yield return new WaitForSeconds(initialDelay);
        
        // Check if we're in the game scene (not the lobby)
        Scene currentScene = SceneManager.GetActiveScene();
        
        Debug.Log($"PlayerClientHandler is in scene: {currentScene.name}");
        
        int attemptCount = 0;
        while (!currentScene.name.Contains("Game") && attemptCount < maxRetries)
        {
            Debug.Log($"Waiting for game scene... (in {currentScene.name}, attempt {attemptCount+1})");
            yield return new WaitForSeconds(retryInterval);
            currentScene = SceneManager.GetActiveScene();
            attemptCount++;
        }
        
        if (!currentScene.name.Contains("Game"))
        {
            Debug.LogWarning("Timeout waiting for game scene. NOT proceeding with HUD setup.");
            // Don't create the HUD in the lobby scene
            yield break;
        }
        
        // Now we should be in the game scene, wait a bit more for objects to spawn
        yield return new WaitForSeconds(5.0f);
        
        // Setup camera and HUD only in game scene
        SetupCamera();
        SetupHUD();
        
        Debug.Log($"Player {OwnerClientId} initialization complete in scene: {currentScene.name}");
    }

    private void Update()
    {
        if (!IsOwner) return; // Only the local player should handle input

        if (Input.GetKeyDown(KeyCode.X)) // Press "X" to test XP gain
        {
            Debug.Log($"[XP TEST] Player {OwnerClientId} requesting 50 XP");

            if (XPManager.Instance != null)
            {
                XPManager.Instance.AwardXPToAll(50);
            }
            else
            {
                Debug.LogError("[XP TEST] XPManager instance not found!");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.H)) // Press "H" to test damage
        {
            var mainTower = MainTowerHP.Instance;
            
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
        // First check if we can find a dedicated HUD canvas
        Canvas hudCanvas = null;
        
        // Look for existing HUD_Canvas
        GameObject hudCanvasObj = GameObject.Find("HUD_Canvas");
        if (hudCanvasObj != null)
        {
            hudCanvas = hudCanvasObj.GetComponent<Canvas>();
            Debug.Log("Found existing HUD_Canvas");
        }
        
        // If not found, look for Game-scene canvases
        if (hudCanvas == null)
        {
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in allCanvases)
            {
                // Skip lobby-specific canvases
                if (!canvas.name.Contains("Connection") && 
                    !canvas.name.Contains("Lobby") && 
                    canvas.gameObject.scene.isLoaded)
                {
                    hudCanvas = canvas;
                    Debug.Log($"Using game canvas: {canvas.name}");
                    break;
                }
            }
        }
        
        // Create new canvas if needed
        if (hudCanvas == null)
        {
            Debug.Log("Creating new HUD_Canvas");
            GameObject newCanvasObj = new GameObject("HUD_Canvas");
            hudCanvas = newCanvasObj.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvasObj.AddComponent<CanvasScaler>();
            newCanvasObj.AddComponent<GraphicRaycaster>();
            
            // Move to root of hierarchy for clarity
            newCanvasObj.transform.SetSiblingIndex(0);
        }
        
        // Instantiate HUD prefab
        hudInstance = Instantiate(HUDCanvasPrefab, hudCanvas.transform);
        Debug.Log($"HUD spawned on {hudCanvas.name}");
        
        // Wait for MainTower to be available
        StartCoroutine(WaitForMainTower());
    }
    
    private IEnumerator WaitForMainTower()
    {
        // Get HUD controller reference
        hudController = hudInstance.GetComponent<PlayerHUDController>();
        if (hudController == null)
        {
            Debug.LogError("PlayerHUDController component not found on HUD prefab!");
            yield break;
        }
        
        // Try repeatedly to find MainTower
        MainTowerHP mainTower = null;
        int attempts = 0;
        
        while (mainTower == null && attempts < maxRetries)
        {
            mainTower = MainTowerHP.Instance;
            
            if (mainTower == null)
            {
                Debug.Log($"MainTower not found, retrying... (Attempt {attempts+1}/{maxRetries})");
                yield return new WaitForSeconds(retryInterval);
                attempts++;
            }
        }
        
        if (mainTower != null)
        {
            Debug.Log("MainTower found successfully!");
        }
        else
        {
            Debug.LogWarning("Failed to find MainTower after multiple attempts");
        }
        
        // Initialize HUD with available components
        hudController.Initialize(player.Health, player.Experience, mainTower);
    }

    private void SetupCamera()
    {
        StartCoroutine(WaitForCamera());
    }
    
    private IEnumerator WaitForCamera()
    {
        PlayerCameraFollow_Smooth cameraFollow = null;
        int attempts = 0;
        
        while (cameraFollow == null && attempts < maxRetries)
        {
            cameraFollow = PlayerCameraFollow_Smooth.Instance;
            
            if (cameraFollow == null)
            {
                Debug.Log($"Camera controller not found, retrying... (Attempt {attempts+1}/{maxRetries})");
                yield return new WaitForSeconds(retryInterval);
                attempts++;
            }
        }
        
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(player.transform);
            Debug.Log("Camera target set successfully");
        }
        else
        {
            Debug.LogError("Failed to find camera controller after multiple attempts");
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
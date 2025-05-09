// Location: Core/Player/Components/PlayerClientHandler.cs
using UnityEngine;
using System;
using Unity.Netcode;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Core.Towers.MainTower;
using Core.GameState;
using Core.GameManagement;
using Core.Player.Base;
using Core.Player.UI;

namespace Core.Player.Components
{
    public class PlayerClientHandler : NetworkBehaviour
    {
        private GameObject hudInstance;
        private PlayerHUDController hudController;
        private GameObject gameOverUIInstance;
        private GameOverUI gameOverUIController;

        public GameObject HUDCanvasPrefab; // Assign this in the inspector
        public GameObject GameOverCanvasPrefab; // Assign this in the inspector
        private PlayerEntity player;
        
        // Add delay configuration
        [SerializeField] private float initialDelay = 1.0f;
        [SerializeField] private float retryInterval = 0.5f;
        [SerializeField] private int maxRetries = 50;
        
        private bool hasInitialized = false;
        private Canvas hudCanvas;
        
        // Add this field to track if we've already subscribed to events
        private bool subscribedToStateEvents = false;

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
            
            // Setup camera and HUD only in game scene
            SetupCamera();
            SetupHUD();
            SetupGameOverUI();
            
            Debug.Log($"Player {OwnerClientId} initialization complete in scene: {currentScene.name}");
        }
        
        // Separate method for subscribing to events
        private void SubscribeToGameStateEvents()
        {
            if (subscribedToStateEvents)
            {
                return; // Already subscribed
            }
            
            // Subscribe to GameStateManager changes
            GameStateManager stateManager = GameServices.Get<GameStateManager>();
            if (stateManager != null)
            {
                stateManager.OnGameStateChanged += OnGameStateChanged;
                Debug.Log("[PlayerClientHandler] Subscribed to GameStateManager events");
                subscribedToStateEvents = true;
                
                // Check current state immediately
                GameStateType currentState = stateManager.CurrentStateType;
                Debug.Log($"[PlayerClientHandler] Current game state is: {currentState}");
                
                // If we're already in GameOver state, show UI right away
                if (currentState == GameStateType.GameOver)
                {
                    Debug.Log("[PlayerClientHandler] Already in GameOver state, showing UI immediately");
                    GameManager gameManager = GameServices.Get<GameManager>();
                    bool isVictory = gameManager != null ? gameManager.IsVictory() : false;
                    ShowGameOverUIDirectly(isVictory);
                }
            }
            else
            {
                Debug.LogWarning("[PlayerClientHandler] Could not find GameStateManager to subscribe to events");
            }
        }

        private void OnGameStateChanged(GameStateType newState)
        {
            Debug.Log($"[PlayerClientHandler] Game state changed to: {newState}");
            
            if (newState == GameStateType.GameOver)
            {
                Debug.Log("[PlayerClientHandler] GameOver state detected, showing UI");
                
                // Get victory state from GameManager
                GameManager gameManager = GameServices.Get<GameManager>();
                bool isVictory = gameManager != null ? gameManager.IsVictory() : false;
                
                // Try to show the UI
                ShowGameOverUIDirectly(isVictory);
            }
            else if (newState == GameStateType.Lobby)
            {
                Debug.Log("[PlayerClientHandler] Lobby state detected, hiding GameOverUI");
                HideGameOverUI();
            }
        }
        
        // Add this method to directly show the GameOver UI 
        public void ShowGameOverUIDirectly(bool victory)
        {
            Debug.Log($"[PlayerClientHandler] Directly showing GameOverUI (Victory: {victory})");
            
            if (gameOverUIController != null && gameOverUIInstance != null)
            {
                gameOverUIInstance.SetActive(true);
                gameOverUIController.SetResult(victory);
                Debug.Log("[PlayerClientHandler] Successfully activated GameOverUI");
            }
            else
            {
                Debug.LogError("[PlayerClientHandler] GameOverUI controller or instance is null!");
                
                // Try to find GameOverCanvas in the scene as a fallback
                Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (var canvas in allCanvases)
                {
                    if (canvas.name.Contains("GameOver"))
                    {
                        canvas.gameObject.SetActive(true);
                        Debug.Log($"[PlayerClientHandler] Found and activated {canvas.name}");
                        
                        // Try to set the result if it has a GameOverUI component
                        GameOverUI ui = canvas.GetComponent<GameOverUI>();
                        if (ui != null)
                        {
                            ui.SetResult(victory);
                        }
                        
                        break;
                    }
                }
            }
        }
        
        private void HideGameOverUI()
        {
            Debug.Log("[PlayerClientHandler] Hiding GameOverUI");
            
            if (gameOverUIController != null && gameOverUIInstance != null)
            {
                gameOverUIInstance.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsOwner) return; // Only the local player should handle input

            if (UnityEngine.Input.GetKeyDown(KeyCode.X)) // Press "X" to test XP gain
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
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.H)) // Press "H" to test damage
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
            
            // For testing GameOver UI
            if (UnityEngine.Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("[UI TEST] Toggling GameOver UI");
                if (gameOverUIController != null && gameOverUIInstance != null)
                {
                    if (gameOverUIInstance.activeSelf)
                        gameOverUIInstance.SetActive(false);
                    else
                        ShowGameOverUIDirectly(true);
                }
                else
                {
                    Debug.LogError("[UI TEST] GameOverUI not initialized yet!");
                }
            }
        }

        private void SetupHUD()
        {
            // First check if we can find a dedicated HUD canvas
            hudCanvas = null;
            
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
                Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
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
        
        private void SetupGameOverUI()
        {
            Debug.Log("[PlayerClientHandler] Setting up GameOverUI");
            
            // Check if we already have a GameOverCanvas in the scene
            GameObject existingCanvas = GameObject.Find("GameOverCanvas(Clone)");
            if (existingCanvas != null)
            {
                Debug.Log("[PlayerClientHandler] Found existing GameOverCanvas");
                gameOverUIInstance = existingCanvas;
                gameOverUIController = existingCanvas.GetComponent<GameOverUI>();
                
                // Make sure it's initially hidden
                if (gameOverUIInstance != null)
                {
                    gameOverUIInstance.SetActive(false);
                }
                
                // Try to subscribe to state events if we haven't already
                if (!subscribedToStateEvents)
                {
                    SubscribeToGameStateEvents();
                }
                
                return;
            }
            
            // Check if GameOverCanvasPrefab is assigned
            if (GameOverCanvasPrefab == null)
            {
                Debug.LogError("[PlayerClientHandler] GameOverCanvasPrefab is not assigned in the Inspector!");
                return;
            }
            
            // Instantiate GameOver UI at the root level, not as a child of HUD_Canvas
            gameOverUIInstance = Instantiate(GameOverCanvasPrefab);
            gameOverUIInstance.name = "GameOverCanvas(Clone)";
            gameOverUIController = gameOverUIInstance.GetComponent<GameOverUI>();
            
            if (gameOverUIController == null)
            {
                Debug.LogError("[PlayerClientHandler] GameOverUI component not found on the prefab!");
            }
            else
            {
                // Make sure it starts hidden
                gameOverUIInstance.SetActive(false);
                Debug.Log("[PlayerClientHandler] GameOverUI setup completed successfully");
                
                // Subscribe to state events
                SubscribeToGameStateEvents();
            }
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
            // Unsubscribe from GameStateManager
            if (subscribedToStateEvents)
            {
                GameStateManager stateManager = GameServices.Get<GameStateManager>();
                if (stateManager != null)
                {
                    stateManager.OnGameStateChanged -= OnGameStateChanged;
                }
                subscribedToStateEvents = false;
            }
            
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
}
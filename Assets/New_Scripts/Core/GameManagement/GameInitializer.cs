using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Core.GameState;
using Core.Towers.MainTower;
using Core.WaveSystem;
using Player.Base;

namespace Core.GameManagement
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Initialization Mode")]
        [SerializeField] private bool isBootstrapScene = true;
        [SerializeField] private bool persistAcrossScenes = true;
        
        [Header("UI References")]
        [SerializeField] private GameObject loginCanvasPrefab; // Single canvas prefab
        private GameObject loginCanvasInstance;
        private GameObject connectionPanel; // Will be looked up from the canvas
        private GameObject lobbyPanel; // Will be looked up from the canvas
        
        [Header("System Prefabs")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject gameStateManagerPrefab;
        [SerializeField] private GameObject networkLobbyManagerPrefab;
        
        [Header("Game Scene Systems")]
        [SerializeField] private GameObject waveManagerPrefab;
        [SerializeField] private GameObject objectPoolPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject playerHUDPrefab; // Add reference to HUD prefab
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] playerSpawnPoints;
        
        // Track initialization state
        private bool hasInitializedNetworking = false;
        private bool hasInitializedGameSystems = false;
        private NetworkLobbyManager lobbyManager;
        
        private void Awake()
        {
            Debug.Log($"[GameInitializer] Starting in {(isBootstrapScene ? "bootstrap" : "game")} scene mode");
            
            // Auto-detect bootstrap scene based on scene name if needed
            if (string.IsNullOrEmpty(SceneManager.GetActiveScene().name))
            {
                isBootstrapScene = SceneManager.GetActiveScene().name.Contains("Bootstrap") || 
                                  SceneManager.GetActiveScene().name.Contains("Lobby");
            }
            
            if (isBootstrapScene && persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        
        private void Start()
        {
            if (isBootstrapScene)
            {
                // Initialize UI for bootstrap scene
                InitializeLoginUI();
                
                // Initialize networking
                InitializeNetworking();
            }
            else
            {
                // Game scene - initialize systems after a short delay
                StartCoroutine(InitializeGameSceneWithDelay(0.5f));
            }
        }
        
        private void InitializeLoginUI()
        {
            // Instantiate login canvas if it doesn't exist
            if (loginCanvasPrefab != null && loginCanvasInstance == null)
            {
                loginCanvasInstance = Instantiate(loginCanvasPrefab);
                DontDestroyOnLoad(loginCanvasInstance);
                
                // Find references to panels
                connectionPanel = loginCanvasInstance.transform.Find("ConnectionPanel")?.gameObject;
                lobbyPanel = loginCanvasInstance.transform.Find("LobbyPanel")?.gameObject;
                
                // Show connection panel by default
                if (connectionPanel) connectionPanel.SetActive(true);
                if (lobbyPanel) lobbyPanel.SetActive(false);
                
                Debug.Log("[GameInitializer] Login UI initialized");
            }
            else
            {
                Debug.LogError("[GameInitializer] LoginCanvas prefab is missing!");
            }
        }
        
        private void InitializeNetworking()
        {
            if (hasInitializedNetworking) return;
            
            Debug.Log("[GameInitializer] Initializing networking");
            
            // Try to subscribe to network events
            if (NetworkManager.Singleton != null)
            {
                ConfigureNetworkManager();
                SubscribeToNetworkEvents();
                hasInitializedNetworking = true;
            }
            else
            {
                Debug.LogWarning("[GameInitializer] NetworkManager not available, retrying...");
                Invoke("InitializeNetworking", 0.5f);
            }
        }
        
        private void ConfigureNetworkManager()
        {
            // Configure NetworkManager for scene management
            NetworkManager.Singleton.NetworkConfig.EnableSceneManagement = true;
            
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(NetworkManager.Singleton.gameObject);
            }
            
            Debug.Log("[GameInitializer] NetworkManager configured");
        }
        
        private void SubscribeToNetworkEvents()
        {
            NetworkManager.Singleton.OnServerStarted += OnNetworkServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnNetworkServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                
                if (NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
                }
            }
            
            // Unsubscribe from LobbyManager events
            if (lobbyManager != null)
            {
                lobbyManager.UnregisterGameSceneLoadedCallback(SpawnPlayersInGameScene);
            }

        }
        
        #region Event Handlers
        
        private void OnNetworkServerStarted()
        {
            Debug.Log("[GameInitializer] Server started");
            
            if (!NetworkManager.Singleton.IsServer) return;
            
            // Spawn core networking systems
            SpawnCoreSystems();
        }
        
        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[GameInitializer] Client connected: {clientId}");
            
            // Switch to lobby UI for clients
            if (isBootstrapScene && NetworkManager.Singleton.LocalClientId == clientId)
            {
                // Show lobby UI for this client
                ShowLobbyUI();
            }
            
            // Notify lobby manager about the new client
            if (NetworkManager.Singleton.IsServer)
            {
                // Get the lobby manager (might be new)
                lobbyManager = GameServices.Get<NetworkLobbyManager>();
                if (lobbyManager != null)
                {
                    lobbyManager.PlayerConnected(clientId);
                }
            }
        }
        
        private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            Debug.Log($"[GameInitializer] Scene load completed: {sceneName}");
            
            // Hide lobby UI when game scene loads
            if (!sceneName.Contains("Bootstrap") && !sceneName.Contains("Lobby") && lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
            
            // Initialize systems if this is the game scene
            if (!sceneName.Contains("Bootstrap") && !isBootstrapScene)
            {
                StartCoroutine(InitializeGameSceneWithDelay(0.2f));
            }
            
            // Notify lobby manager about scene load
            lobbyManager = GameServices.Get<NetworkLobbyManager>();
            if (lobbyManager != null)
            {
                // Just perform the spawning here - the NetworkLobbyManager will trigger its own event
                // when appropriate, which will call our registered callback
                if (sceneName == "GameScene" || sceneName.Contains("Game"))
                {
                    // Only call spawn directly if we're in a Game scene
                    Debug.Log("[GameInitializer] Detected game scene, ready for player spawning");
                    // No need to do anything - our registered callback will be called
                }
            }
        }
        
        #endregion
        
        #region Bootstrap Scene Methods
        private void SpawnCoreSystems()
        {
            // Only spawn objects if they don't already exist
            
            // Spawn GameStateManager
            GameStateManager stateManager = GameServices.Get<GameStateManager>();
            if (stateManager == null && gameStateManagerPrefab != null)
            {
                SpawnNetworkObject(gameStateManagerPrefab, "GameStateManager");
            }
            
            // Spawn NetworkLobbyManager
            lobbyManager = GameServices.Get<NetworkLobbyManager>();
            if (lobbyManager == null && networkLobbyManagerPrefab != null)
            {
                SpawnNetworkObject(networkLobbyManagerPrefab, "NetworkLobbyManager");
                
                // Re-get the reference
                lobbyManager = GameServices.Get<NetworkLobbyManager>();
            }

            // Register the callback for player spawning - ONLY DO IT ONCE HERE
            if (lobbyManager != null)
            {
                lobbyManager.RegisterGameSceneLoadedCallback(SpawnPlayersInGameScene);
                Debug.Log("Registered player spawn callback with NetworkLobbyManager");
            }
            
            // Spawn GameManager
            GameManager gameManager = GameServices.Get<GameManager>();
            if (gameManager == null && gameManagerPrefab != null)
            {
                SpawnNetworkObject(gameManagerPrefab, "GameManager");
            }
            
            // Show lobby UI for host/server
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                ShowLobbyUI();
            }
        }
        
        private void ShowLobbyUI()
        {
            if (connectionPanel) connectionPanel.SetActive(false);
            if (lobbyPanel) lobbyPanel.SetActive(true);
        }
        
        // Method to spawn players in the game scene
        private void SpawnPlayersInGameScene()
        {
            Debug.Log("[GameInitializer] Spawning players in game scene");
        }
        
        #endregion
        
        #region Game Scene Methods
        
        private IEnumerator InitializeGameSceneWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            InitializeGameScene();
        }
        
        private void InitializeGameScene()
        {
            if (hasInitializedGameSystems) return;
            hasInitializedGameSystems = true;
            
            Debug.Log("[GameInitializer] Initializing game scene");
            
            // Server-specific initialization
            if (NetworkManager.Singleton.IsServer)
            {
                SpawnGameSystems();
            }
            
            // Client-specific initialization
            if (NetworkManager.Singleton.IsClient)
            {
                // DO NOT initialize player HUD here - it will be done by GameSceneInitializer
                
                // Connect the camera to the player
                StartCoroutine(SetupPlayerCamera());
            }
        }
        
        private void SpawnGameSystems()
        {
            Debug.Log("[GameInitializer] Spawning game systems");
            
            // Spawn WaveManager if not already present
            WaveManager waveManager = GameServices.Get<WaveManager>();
            if (waveManager == null && waveManagerPrefab != null)
            {
                SpawnNetworkObject(waveManagerPrefab, "WaveManager");
            }
            
            // Spawn ObjectPool if not already present
            NetworkObjectPool objectPool = GameServices.Get<NetworkObjectPool>();
            if (objectPool == null && objectPoolPrefab != null)
            {
                SpawnNetworkObject(objectPoolPrefab, "NetworkObjectPool");
            }
            
            // Connect systems by finding the tower and connecting it to GameManager
            StartCoroutine(ConnectGameManager());
        }
        
        private IEnumerator ConnectGameManager()
        {
            // Wait a moment for objects to be initialized
            yield return new WaitForSeconds(0.5f);
            
            GameManager gameManager = GameServices.Get<GameManager>();
            MainTowerHP mainTower = FindObjectOfType<MainTowerHP>();
            
            if (gameManager != null && mainTower != null)
            {
                gameManager.ConnectMainTower(mainTower);
            }
            
            // Connect to wave manager
            WaveManager waveManager = GameServices.Get<WaveManager>();
            if (gameManager != null && waveManager != null)
            {
                gameManager.ConnectWaveManager();
            }
        }
        
        private IEnumerator SetupPlayerCamera()
        {
            PlayerCameraFollow_Smooth cameraFollow = FindObjectOfType<PlayerCameraFollow_Smooth>();
            if (cameraFollow == null) yield break;
            
            // Wait until local player exists
            while (NetworkManager.Singleton.LocalClient == null || 
                   NetworkManager.Singleton.LocalClient.PlayerObject == null)
            {
                yield return null;
            }
            
            // Get local player GameObject
            GameObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
            
            // Set camera target
            if (localPlayer != null)
            {
                cameraFollow.SetTarget(localPlayer.transform);
                Debug.Log("[GameInitializer] Setup player camera follow");
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private void SpawnNetworkObject(GameObject prefab, string name)
        {
            if (prefab == null)
            {
                Debug.LogError($"[GameInitializer] {name} prefab is null!");
                return;
            }
            
            GameObject obj = Instantiate(prefab);
            NetworkObject networkObject = obj.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.Spawn();
                Debug.Log($"[GameInitializer] {name} spawned successfully");
            }
            else
            {
                Debug.LogError($"[GameInitializer] {name} prefab is missing NetworkObject component!");
                Destroy(obj);
            }
        }
        
        #endregion
    }
}
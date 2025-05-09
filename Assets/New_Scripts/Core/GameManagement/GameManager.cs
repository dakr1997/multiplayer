using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Core.GameState;
using Core.Towers.MainTower; // Add this namespace reference for MainTowerHP
using Core.WaveSystem; // Reference for WaveManager

namespace Core.GameManagement
{
    /// <summary>
    /// Central GameManager that orchestrates the game flow and systems.
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        // Singleton pattern
        public static GameManager Instance { get; private set; }
        
        [Header("Game Configuration")]
        [SerializeField] private int minPlayersToStart = 2;
        [SerializeField] private float buildingPhaseDuration = 60f;
        [SerializeField] private int maxWaves = 5;
        
        [Header("Main Tower")]
        [SerializeField] private MainTowerHP mainTower;
        
        // Network variables
        private NetworkVariable<int> currentWave = new NetworkVariable<int>(0);
        private NetworkVariable<float> gameTime = new NetworkVariable<float>(0);
        private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> isVictory = new NetworkVariable<bool>(false);
        
        // System references
        private GameStateManager stateManager;
        private WaveManager waveManager;
        private NetworkLobbyManager lobbyManager;
        
        // Coroutines
        private Coroutine buildingPhaseCoroutine;
        
        // Events
        public event Action OnGameStarted;
        public event Action<bool> OnGameEnded; // bool indicates victory
        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnBuildingPhaseStarted;
        public event Action<int> OnBuildingTimeUpdated;
        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[GameManager] Initialized");
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Register with service locator
            GameServices.Register<GameManager>(this);
            
            // Find references to other systems
            FindAndConnectSystems();
            
            // Subscribe to tower destroyed event immediately
            MainTowerHP.OnTowerDestroyed += HandleMainTowerDestroyed;
        }
        
        private void Start()
        {
            // Double-check that we're connected to MainTowerHP
            if (mainTower == null)
            {
                mainTower = FindObjectOfType<MainTowerHP>();
                if (mainTower != null)
                {
                    ConnectMainTower(mainTower);
                }
            }
        }
        
        private void FindAndConnectSystems()
        {
            // Find GameStateManager
            stateManager = GameServices.Get<GameStateManager>();
            if (stateManager != null)
            {
                Debug.Log("[GameManager] Connected to GameStateManager");
                stateManager.OnGameStateChanged += OnGameStateChanged;
            }
            else
            {
                Debug.LogWarning("[GameManager] GameStateManager not found!");
                // Try again later
                StartCoroutine(RetryFindSystems());
            }
            
            // Find NetworkLobbyManager
            lobbyManager = GameServices.Get<NetworkLobbyManager>();
            if (lobbyManager != null)
            {
                Debug.Log("[GameManager] Connected to NetworkLobbyManager");
                // No need to connect events here, we'll integrate with lobby state
            }
            
            // Find WaveManager - this might be null initially since it's scene-specific
            waveManager = GameServices.Get<WaveManager>();
            if (waveManager != null)
            {
                ConnectWaveManager();
            }
        }
        
        private IEnumerator RetryFindSystems()
        {
            yield return new WaitForSeconds(0.5f);
            FindAndConnectSystems();
        }
        
        private void Update()
        {
            if (IsServer && isGameActive.Value)
            {
                // Track game time
                gameTime.Value += Time.deltaTime;
            }
        }
        
        /// <summary>
        /// Connect to WaveManager when it becomes available
        /// </summary>
        public void ConnectWaveManager()
        {
            if (!IsServer) return;
            
            if (waveManager == null)
            {
                waveManager = GameServices.Get<WaveManager>();
                if (waveManager == null) return;
            }
            
            Debug.Log("[GameManager] Connected to WaveManager");
            
            // Subscribe to wave events - using static events from WaveManager
            WaveManager.OnWaveCompleted += HandleWaveCompleted;
            WaveManager.OnAllWavesCompleted += HandleAllWavesCompleted;
        }
        
        /// <summary>
        /// Connect to MainTower when it becomes available
        /// </summary>
        public void ConnectMainTower(MainTowerHP tower)
        {
            if (!IsServer) return;
            
            if (tower == null && mainTower == null)
            {
                // Try to find it in the scene
                mainTower = FindObjectOfType<MainTowerHP>();
                if (mainTower == null) return;
            }
            else if (tower != null)
            {
                mainTower = tower;
            }
            
            Debug.Log("[GameManager] Connected to MainTower");
            
            // Make sure we're subscribed to the tower destroyed event
            MainTowerHP.OnTowerDestroyed -= HandleMainTowerDestroyed; // Prevent duplicate subscription
            MainTowerHP.OnTowerDestroyed += HandleMainTowerDestroyed;
            
            Debug.Log("[GameManager] Subscribed to MainTower.OnTowerDestroyed event");
        }
        
        /// <summary>
        /// Public method to be called directly when tower is destroyed
        /// </summary>
        public void OnMainTowerDestroyed()
        {
            if (!IsServer) return;
            
            Debug.Log("[GameManager] OnMainTowerDestroyed called directly");
            
            // End game with defeat
            EndGame(false);
        }
        
        #region State Management
        
        /// <summary>
        /// Handle game state changes
        /// </summary>
        private void OnGameStateChanged(GameStateType newState)
        {
            Debug.Log($"[GameManager] Game state changed to {newState}");
            
            switch (newState)
            {
                case GameStateType.Lobby:
                    HandleLobbyState();
                    break;
                    
                case GameStateType.Building:
                    HandleBuildingState();
                    break;
                    
                case GameStateType.Wave:
                    HandleWaveState();
                    break;
                    
                case GameStateType.GameOver:
                    HandleGameOverState();
                    break;
            }
        }
        
        private void HandleLobbyState()
        {
            // Reset game variables
            if (IsServer)
            {
                currentWave.Value = 0;
                gameTime.Value = 0;
                isGameActive.Value = false;
                isVictory.Value = false;
            }
            
            // Re-enable player controls on all clients
            EnablePlayerControlsClientRpc();
        }
        
        private void HandleBuildingState()
        {
            if (!IsServer) return;
            
            Debug.Log("[GameManager] Starting building phase");
            
            // Start building phase countdown
            if (buildingPhaseCoroutine != null)
            {
                StopCoroutine(buildingPhaseCoroutine);
            }
            
            buildingPhaseCoroutine = StartCoroutine(BuildingPhaseCountdown());
            
            // Notify listeners
            OnBuildingPhaseStarted?.Invoke();
        }
        
        private void HandleWaveState()
        {
            if (!IsServer) return;
            
            // Increment wave counter and start the wave
            currentWave.Value++;
            
            Debug.Log($"[GameManager] Starting wave {currentWave.Value}");
            
            // Tell wave manager to start the wave
            if (waveManager != null)
            {
                waveManager.StartWave(currentWave.Value);
            }
            else
            {
                Debug.LogWarning("[GameManager] WaveManager not found when starting wave!");
                
                // Try to find wave manager
                waveManager = GameServices.Get<WaveManager>();
                if (waveManager != null)
                {
                    ConnectWaveManager();
                    waveManager.StartWave(currentWave.Value);
                }
            }
            
            // Notify listeners
            OnWaveStarted?.Invoke(currentWave.Value);
        }
        
        private void HandleGameOverState()
        {
            Debug.Log($"[GameManager] Game over! Victory: {isVictory.Value}");
            
            // Stop any running coroutines
            if (buildingPhaseCoroutine != null)
            {
                StopCoroutine(buildingPhaseCoroutine);
                buildingPhaseCoroutine = null;
            }
            
            // Show GameOverUI on all clients
            ShowGameOverUIClientRpc(isVictory.Value);
            
            // Notify listeners
            OnGameEnded?.Invoke(isVictory.Value);
        }
        
        #endregion
        
        #region Game Flow Methods
        
        /// <summary>
        /// Start a new game
        /// </summary>
        public void StartGame()
        {
            if (!IsServer) return;
            
            Debug.Log("[GameManager] Starting game");
            
            // Reset game state
            currentWave.Value = 0;
            gameTime.Value = 0;
            isGameActive.Value = true;
            
            // Make sure tower is reset if needed
            if (mainTower != null)
            {
                mainTower.Reset();
            }
            
            // Change to building state to begin the game
            if (stateManager != null)
            {
                stateManager.ChangeState(GameStateType.Building);
            }
            
            // Notify listeners
            OnGameStarted?.Invoke();
        }
        
        /// <summary>
        /// Return to lobby - called from GameOverUI
        /// </summary>
/// <summary>
/// Return to lobby - called from GameOverUI
/// </summary>
    public void ReturnToLobby()
    {
        Debug.Log("[GameManager] Return to lobby requested");
        
        // If client, request server to handle return to lobby
        if (!IsServer) 
        {
            Debug.Log("[GameManager] Client requesting server to return to lobby");
            RequestReturnToLobbyServerRpc();
            return;
        }
        
        // Get the NetworkLobbyManager to handle clean disconnect and scene transition
        NetworkLobbyManager lobbyManager = GameServices.Get<NetworkLobbyManager>();
        if (lobbyManager != null)
        {
            Debug.Log("[GameManager] Found NetworkLobbyManager, calling DisconnectAndResetGame");
            lobbyManager.DisconnectAndResetGame();
            return;
        }
        
        // If no NetworkLobbyManager is found, fall back to basic state reset
        Debug.LogWarning("[GameManager] NetworkLobbyManager not found, using fallback method");
        
        // Reset game variables
        currentWave.Value = 0;
        gameTime.Value = 0;
        isGameActive.Value = false;
        isVictory.Value = false;
        
        // Reset tower if it exists
        if (mainTower != null)
        {
            mainTower.Reset();
        }
        
        // Change to lobby state
        if (stateManager != null)
        {
            stateManager.ChangeState(GameStateType.Lobby);
        }
        
        // Re-enable controls
        EnablePlayerControlsClientRpc();
    }
        
        [ServerRpc(RequireOwnership = false)]
        public void RequestReturnToLobbyServerRpc(ServerRpcParams serverRpcParams = default)
        {
            Debug.Log("[GameManager] Client requested return to lobby");
            ReturnToLobby();
        }
        
        public void EndGame(bool victory)
        {
            if (!IsServer) return;
            
            Debug.Log($"[GameManager] Ending game. Victory: {victory}");
            
            isGameActive.Value = false;
            isVictory.Value = victory;
            
            // Notify all clients about the game over to disable controls
            NotifyGameOverClientRpc(victory);
            
            // Get a reference to the state manager
            if (stateManager == null)
            {
                stateManager = GameServices.Get<GameStateManager>();
            }
            
            if (stateManager != null)
            {
                Debug.Log($"[GameManager] Forcing state change to GameOver. Current state: {stateManager.CurrentStateType}");
                
                // Stop any coroutines that might interfere with state change
                StopAllCoroutines();
                
                // Force state change to GameOver
                stateManager.ChangeState(GameStateType.GameOver);
                
                // Double-check that the state changed successfully
                StartCoroutine(CheckStateChangeSuccess(GameStateType.GameOver));
            }
            else
            {
                Debug.LogError("[GameManager] Could not find GameStateManager to change state!");
            }
        }
        
        private IEnumerator CheckStateChangeSuccess(GameStateType expectedState)
        {
            yield return null; // Wait a frame for state to update
            
            if (stateManager != null && stateManager.CurrentStateType == expectedState)
            {
                Debug.Log($"[GameManager] Successfully changed to {expectedState} state");
            }
            else if (stateManager != null)
            {
                Debug.LogError($"[GameManager] Failed to change to {expectedState} state! Current state is still {stateManager.CurrentStateType}");
                
                // Try one more time
                stateManager.ChangeState(expectedState);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle when a wave is completed
        /// </summary>
        private void HandleWaveCompleted(int waveNumber)
        {
            if (!IsServer) return;
            
            Debug.Log($"[GameManager] Wave {waveNumber} completed");
            
            // Notify listeners
            OnWaveCompleted?.Invoke(waveNumber);
            
            // Change to building state
            if (stateManager != null)
            {
                stateManager.ChangeState(GameStateType.Building);
            }
        }
        
        /// <summary>
        /// Handle when all waves are completed (victory)
        /// </summary>
        private void HandleAllWavesCompleted()
        {
            if (!IsServer) return;
            
            Debug.Log("[GameManager] All waves completed - Victory!");
            
            // End game with victory
            EndGame(true);
        }
        
        /// <summary>
        /// Handle when the main tower is destroyed (defeat)
        /// </summary>
        private void HandleMainTowerDestroyed()
        {
            if (!IsServer) return;
            
            Debug.Log("[GameManager] Main tower destroyed - Defeat! Event handler called.");
            
            // End game with defeat
            EndGame(false);
        }
        
        #endregion
        
        #region Player Controls
        
        private void DisablePlayerControls()
        {
            // Find and disable all player movement scripts
            PlayerMovement[] playerMovements = FindObjectsOfType<PlayerMovement>();
            foreach (var movement in playerMovements)
            {
                movement.enabled = false;
            }
            
            // Also disable player damage scripts
            PlayerDamage[] playerDamages = FindObjectsOfType<PlayerDamage>();
            foreach (var damage in playerDamages)
            {
                damage.enabled = false;
            }
            
            Debug.Log("[GameManager] Player controls disabled for Game Over state");
        }
        
        private void EnablePlayerControls()
        {
            // Find and enable all player movement scripts
            PlayerMovement[] playerMovements = FindObjectsOfType<PlayerMovement>();
            foreach (var movement in playerMovements)
            {
                movement.enabled = true;
            }
            
            // Also enable player damage scripts
            PlayerDamage[] playerDamages = FindObjectsOfType<PlayerDamage>();
            foreach (var damage in playerDamages)
            {
                damage.enabled = true;
            }
            
            Debug.Log("[GameManager] Player controls enabled");
        }
        
        #endregion
        
        #region Coroutines
        
        /// <summary>
        /// Countdown for the building phase
        /// </summary>
        private IEnumerator BuildingPhaseCountdown()
        {
            float remainingTime = buildingPhaseDuration;
            
            while (remainingTime > 0)
            {
                // Update clients with countdown every second
                if (remainingTime % 1 < 0.1f)
                {
                    int seconds = Mathf.FloorToInt(remainingTime);
                    UpdateBuildingTimeClientRpc(seconds);
                    OnBuildingTimeUpdated?.Invoke(seconds);
                }
                
                remainingTime -= Time.deltaTime;
                yield return null;
            }
            
            // Building phase is over, start the wave
            if (stateManager != null && isGameActive.Value)
            {
                stateManager.ChangeState(GameStateType.Wave);
            }
        }
        
        #endregion
        
        #region RPCs
        
        [ClientRpc]
        private void UpdateBuildingTimeClientRpc(int seconds)
        {
            // This will be received by all clients
            Debug.Log($"[GameManager] Building phase: {seconds} seconds remaining");
            
            // Local event for UI to subscribe to
            OnBuildingTimeUpdated?.Invoke(seconds);
        }
        
        [ClientRpc]
        private void NotifyGameOverClientRpc(bool victory)
        {
            Debug.Log($"[GameManager] Game Over notification received by client. Victory: {victory}");
            
            // Disable player movement and controls
            DisablePlayerControls();
        }
        
        [ClientRpc]
        private void EnablePlayerControlsClientRpc()
        {
            Debug.Log($"[GameManager] Re-enabling player controls on client");
            
            // Enable player movement and controls
            EnablePlayerControls();
        }
        
        [ClientRpc]
        private void ShowGameOverUIClientRpc(bool victory)
        {
            Debug.Log($"[GameManager] ShowGameOverUIClientRpc called with victory={victory}");
            
            // Find all GameOverCanvas objects in the scene
            Canvas[] allCanvases = FindObjectsOfType<Canvas>(true); // Include inactive objects
            bool foundCanvas = false;
            
            foreach (var canvas in allCanvases)
            {
                if (canvas.name.Contains("GameOver"))
                {
                    // Activate the canvas
                    canvas.gameObject.SetActive(true);
                    
                    // Try to set the victory status
                    GameOverUI gameOverUI = canvas.GetComponent<GameOverUI>();
                    if (gameOverUI != null)
                    {
                        gameOverUI.SetResult(victory);
                        Debug.Log($"[GameManager] Set GameOverUI result: {(victory ? "Victory!" : "Defeat!")}");
                    }
                    
                    foundCanvas = true;
                    break;
                }
            }
            
            if (!foundCanvas)
            {
                Debug.LogError("[GameManager] Could not find any GameOverCanvas in the scene!");
                
                // Try to find any PlayerClientHandler to show UI
                PlayerClientHandler[] handlers = FindObjectsOfType<PlayerClientHandler>();
                foreach (var handler in handlers)
                {
                    if (handler.IsOwner) // Only the local player's handler
                    {
                        Debug.Log("[GameManager] Trying to show GameOverUI via PlayerClientHandler");
                        handler.SendMessage("ShowGameOverUIDirectly", victory, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get the current wave number
        /// </summary>
        public int GetCurrentWave()
        {
            return currentWave.Value;
        }
        
        /// <summary>
        /// Get the elapsed game time
        /// </summary>
        public float GetGameTime()
        {
            return gameTime.Value;
        }
        
        /// <summary>
        /// Check if the game is currently active
        /// </summary>
        public bool IsGameActive()
        {
            return isGameActive.Value;
        }
        
        /// <summary>
        /// Check if the game ended in victory
        /// </summary>
        public bool IsVictory()
        {
            return isVictory.Value;
        }
        
        #endregion
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            // Unsubscribe from events
            if (stateManager != null)
            {
                stateManager.OnGameStateChanged -= OnGameStateChanged;
            }
            
            // Unsubscribe from static events
            WaveManager.OnWaveCompleted -= HandleWaveCompleted;
            WaveManager.OnAllWavesCompleted -= HandleAllWavesCompleted;
            MainTowerHP.OnTowerDestroyed -= HandleMainTowerDestroyed;
            
            // Unregister from service locator
            GameServices.Unregister<GameManager>();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from static events to prevent memory leaks
            MainTowerHP.OnTowerDestroyed -= HandleMainTowerDestroyed;
            
            // We should simply unsubscribe without checking, as we don't have access
            // to check if the event has subscribers outside the declaring class
            WaveManager.OnWaveCompleted -= HandleWaveCompleted;
            WaveManager.OnAllWavesCompleted -= HandleAllWavesCompleted;
        }
    }
}
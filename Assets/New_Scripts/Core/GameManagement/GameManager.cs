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
            
            // Subscribe to tower destruction event - using static event from MainTowerHP
            MainTowerHP.OnTowerDestroyed += HandleMainTowerDestroyed;
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
            
            // Change to building state to begin the game
            if (stateManager != null)
            {
                stateManager.ChangeState(GameStateType.Building);
            }
            
            // Notify listeners
            OnGameStarted?.Invoke();
        }
        
        /// <summary>
        /// End the game with a win or loss
        /// </summary>
        public void EndGame(bool victory)
        {
            if (!IsServer) return;
            
            Debug.Log($"[GameManager] Ending game. Victory: {victory}");
            
            isGameActive.Value = false;
            isVictory.Value = victory;
            
            // Change to game over state
            if (stateManager != null)
            {
                stateManager.ChangeState(GameStateType.GameOver);
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
            
            Debug.Log("[GameManager] Main tower destroyed - Defeat!");
            
            // End game with defeat
            EndGame(false);
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
    }
}
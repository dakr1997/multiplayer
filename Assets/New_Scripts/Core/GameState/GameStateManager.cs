using UnityEngine;
using Unity.Netcode;
using System;
using Core.GameManagement;

namespace Core.GameState
{
    /// <summary>
    /// Manages game state transitions and current state.
    /// </summary>
    public class GameStateManager : NetworkBehaviour
    {
        // Game states
        private GameState _currentState;
        private WaveState _waveState;
        private BuildingState _buildingState;
        private GameOverState _gameOverState;
        private LobbyState _lobbyState;
        
        // Network variable for current state (for synchronization)
        private NetworkVariable<GameStateType> _networkGameState = new NetworkVariable<GameStateType>(
            GameStateType.Lobby,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        
        // Events
        public delegate void GameStateChangedDelegate(GameStateType newState);
        public event GameStateChangedDelegate OnGameStateChanged;
        
        private void Awake()
        {
            // Initialize states
            _waveState = new WaveState(this);
            _buildingState = new BuildingState(this);
            _gameOverState = new GameOverState(this);
            _lobbyState = new LobbyState(this);
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Register with service locator
            GameServices.Register<GameStateManager>(this);
            
            // Subscribe to network variable changes
            _networkGameState.OnValueChanged += OnNetworkStateChanged;
            
            // Set initial state
            if (IsServer)
            {
                ChangeState(GameStateType.Lobby);
            }
            else
            {
                // Client should sync with current network state
                UpdateStateFromNetwork(_networkGameState.Value);
            }
        }
        
        private void Update()
        {
            // Update current state
            _currentState?.Update();
        }
        
        /// <summary>
        /// Change the current game state. Only the server can initiate state changes.
        /// </summary>
        public void ChangeState(GameStateType newState)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[GameStateManager] Only the server can change game state!");
                return;
            }
            
            if (_networkGameState.Value == newState)
            {
                Debug.Log($"[GameStateManager] Already in state {newState}, ignoring change request");
                return;
            }
            
            Debug.Log($"[GameStateManager] Changing state from {_networkGameState.Value} to {newState}");
            
            // Force exit current state
            if (_currentState != null)
            {
                Debug.Log($"[GameStateManager] Forcing exit of current state: {_networkGameState.Value}");
                _currentState.Exit();
            }
            
            // Update network variable to synchronize with clients
            _networkGameState.Value = newState;
            
            // Apply the state change locally
            UpdateStateFromNetwork(newState);
        }
        
        /// <summary>
        /// Force state change even if already in that state (used for recovery)
        /// </summary>
        public void ForceStateChange(GameStateType newState)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[GameStateManager] Only the server can force state change!");
                return;
            }
            
            Debug.Log($"[GameStateManager] FORCE changing state to {newState}");
            
            // Force exit current state
            if (_currentState != null)
            {
                _currentState.Exit();
            }
            
            // Set state directly first to ensure change
            _networkGameState.Value = newState;
            
            // Apply the state change locally
            UpdateStateFromNetwork(newState);
        }
        
        /// <summary>
        /// Handle network state change.
        /// </summary>
        private void OnNetworkStateChanged(GameStateType oldState, GameStateType newState)
        {
            // Apply state change when network variable changes
            if (!IsServer) // Only clients need to react to this, server already applied the change
            {
                UpdateStateFromNetwork(newState);
            }
        }
        
        /// <summary>
        /// Update local state based on network state.
        /// </summary>
        private void UpdateStateFromNetwork(GameStateType stateType)
        {
            Debug.Log($"[GameStateManager] UpdateStateFromNetwork: {stateType}");
            
            // Exit current state if it exists
            if (_currentState != null)
            {
                Debug.Log($"[GameStateManager] Exiting current state: {_currentState.GetType().Name}");
                _currentState.Exit();
            }
            
            // Set new state
            switch (stateType)
            {
                case GameStateType.Lobby:
                    _currentState = _lobbyState;
                    break;
                case GameStateType.Wave:
                    _currentState = _waveState;
                    break;
                case GameStateType.Building:
                    _currentState = _buildingState;
                    break;
                case GameStateType.GameOver:
                    _currentState = _gameOverState;
                    break;
            }
            
            // Enter new state
            Debug.Log($"[GameStateManager] Entering new state: {_currentState.GetType().Name}");
            _currentState.Enter();
            
            // Trigger event
            OnGameStateChanged?.Invoke(stateType);
            
            Debug.Log($"[GameStateManager] State successfully changed to {stateType}");
        }
        
        /// <summary>
        /// Get the GameOverState for direct access
        /// </summary>
        public GameOverState GetGameOverState()
        {
            return _gameOverState;
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            // Unsubscribe from events
            _networkGameState.OnValueChanged -= OnNetworkStateChanged;
            
            // Unregister from service locator
            GameServices.Unregister<GameStateManager>();
        }
        
        // Public properties
        public GameStateType CurrentStateType => _networkGameState.Value;
        public bool IsInWaveState => _networkGameState.Value == GameStateType.Wave;
        public bool IsInBuildingState => _networkGameState.Value == GameStateType.Building;
    }
}
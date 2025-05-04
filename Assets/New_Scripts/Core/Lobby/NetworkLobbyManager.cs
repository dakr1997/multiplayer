// File: Assets/_Project/Scripts/Core/Lobby/NetworkLobbyManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// Manages the multiplayer lobby.
/// </summary>
public class NetworkLobbyManager : NetworkBehaviour
{
    // Player info tracking
    public class PlayerInfo
    {
        public string PlayerName;
        public bool IsReady;
    }

    // Network variable to track player readiness
    private NetworkVariable<int> _playersReady = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    
    // Network variable to track total players
    private NetworkVariable<int> _totalPlayers = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    
    // Network variable for countdown state
    private NetworkVariable<bool> _countdownActive = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    
    // Countdown timer value
    private NetworkVariable<float> _countdownTimer = new NetworkVariable<float>(10f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    
    // Events
    public event Action<int, int> OnReadyStatusChanged; // (playersReady, totalPlayers)
    public event Action<float> OnCountdownTick; // (remainingTime)
    public event Action OnCountdownComplete;
    public event Action OnPlayerListChanged;
    
    // Dictionary to track player readiness (clientId -> ready)
    private Dictionary<ulong, bool> _playerReadyStatus = new Dictionary<ulong, bool>();
    
    // Dictionary to track player info
    private Dictionary<ulong, PlayerInfo> _playerInfos = new Dictionary<ulong, PlayerInfo>();
    
    // Network list for player info
    private NetworkList<PlayerInfoNetworkData> _playerInfoList;
    
    // Structure for network serialization
    public struct PlayerInfoNetworkData : INetworkSerializable, IEquatable<PlayerInfoNetworkData>
    {
        public ulong ClientId;
        public FixedString32Bytes PlayerName;
        public bool IsReady;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref IsReady);
        }
        
        public bool Equals(PlayerInfoNetworkData other)
        {
            return ClientId == other.ClientId && 
                   PlayerName == other.PlayerName && 
                   IsReady == other.IsReady;
        }
    }
    
    // Reference to the game state manager
    private GameStateManager _gameStateManager;
    
    private void Awake()
    {
        // Initialize network list
        _playerInfoList = new NetworkList<PlayerInfoNetworkData>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Register with service locator
        GameServices.Register<NetworkLobbyManager>(this);
        
        // Find game state manager
        _gameStateManager = GameServices.Get<GameStateManager>();
        
        // Subscribe to network variable changes
        _playersReady.OnValueChanged += (oldValue, newValue) => OnReadyStatusChanged?.Invoke(newValue, _totalPlayers.Value);
        _totalPlayers.OnValueChanged += (oldValue, newValue) => OnReadyStatusChanged?.Invoke(_playersReady.Value, newValue);
        _playerInfoList.OnListChanged += OnPlayerInfoListChanged;
        
        // If we're the server, initialize player count
        if (IsServer)
        {
            _totalPlayers.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
        }
    }
    
    private void Update()
    {
        // Only server updates the countdown timer
        if (IsServer && _countdownActive.Value)
        {
            // Update countdown timer
            _countdownTimer.Value -= Time.deltaTime;
            OnCountdownTick?.Invoke(_countdownTimer.Value);
            
            // Check if countdown is complete
            if (_countdownTimer.Value <= 0)
            {
                _countdownActive.Value = false;
                OnCountdownComplete?.Invoke();
                
                // Start the game
                _gameStateManager.ChangeState(GameStateType.Wave);
            }
        }
    }
    
    /// <summary>
    /// Called by a client to toggle their ready status.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TogglePlayerReadyServerRpc(ulong clientId)
    {
        // Toggle ready status for this player
        if (!_playerReadyStatus.ContainsKey(clientId))
        {
            _playerReadyStatus[clientId] = true;
        }
        else
        {
            _playerReadyStatus[clientId] = !_playerReadyStatus[clientId];
        }
        
        // Update player info
        if (_playerInfos.ContainsKey(clientId))
        {
            _playerInfos[clientId].IsReady = _playerReadyStatus[clientId];
            UpdatePlayerInfoList();
        }
        
        // Count ready players
        int readyCount = 0;
        foreach (var status in _playerReadyStatus.Values)
        {
            if (status) readyCount++;
        }
        
        // Update network variable
        _playersReady.Value = readyCount;
        
        // Check if all players are ready
        CheckForGameStart();
    }
    
    /// <summary>
    /// Set player name
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerNameServerRpc(ulong clientId, string playerName)
    {
        if (!_playerInfos.ContainsKey(clientId))
        {
            _playerInfos[clientId] = new PlayerInfo
            {
                PlayerName = playerName,
                IsReady = false
            };
        }
        else
        {
            _playerInfos[clientId].PlayerName = playerName;
        }
        
        UpdatePlayerInfoList();
    }
    
    /// <summary>
    /// Check if all players are ready to start the game.
    /// </summary>
    private void CheckForGameStart()
    {
        if (IsServer && _playersReady.Value == _totalPlayers.Value && _totalPlayers.Value >= 1)
        {
            // All players are ready, start countdown
            StartCountdown();
        }
    }
    
    /// <summary>
    /// Called when a new player connects.
    /// </summary>
    public void PlayerConnected(ulong clientId)
    {
        if (IsServer)
        {
            // Update total players
            _totalPlayers.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
            
            // Add new player to ready status dictionary (not ready by default)
            if (!_playerReadyStatus.ContainsKey(clientId))
            {
                _playerReadyStatus[clientId] = false;
            }
            
            // Add player info
            if (!_playerInfos.ContainsKey(clientId))
            {
                _playerInfos[clientId] = new PlayerInfo
                {
                    PlayerName = $"Player {clientId}",
                    IsReady = false
                };
                
                UpdatePlayerInfoList();
            }
        }
    }
    
    /// <summary>
    /// Called when a player disconnects.
    /// </summary>
    public void PlayerDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            // Remove player from ready status
            if (_playerReadyStatus.ContainsKey(clientId))
            {
                bool wasReady = _playerReadyStatus[clientId];
                _playerReadyStatus.Remove(clientId);
                
                // Update ready count if needed
                if (wasReady)
                {
                    _playersReady.Value--;
                }
            }
            
            // Remove player info
            if (_playerInfos.ContainsKey(clientId))
            {
                _playerInfos.Remove(clientId);
                UpdatePlayerInfoList();
            }
            
            // Update total players
            _totalPlayers.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
        }
    }
    
    /// <summary>
    /// Update the player info list
    /// </summary>
    private void UpdatePlayerInfoList()
    {
        if (!IsServer) return;
        
        _playerInfoList.Clear();
        foreach (var kvp in _playerInfos)
        {
            _playerInfoList.Add(new PlayerInfoNetworkData
            {
                ClientId = kvp.Key,
                PlayerName = new FixedString32Bytes(kvp.Value.PlayerName),
                IsReady = kvp.Value.IsReady
            });
        }
    }
    
    /// <summary>
    /// Handle player info list changes
    /// </summary>
    private void OnPlayerInfoListChanged(NetworkListEvent<PlayerInfoNetworkData> changeEvent)
    {
        OnPlayerListChanged?.Invoke();
    }
    
    /// <summary>
    /// Start the countdown to begin the game.
    /// </summary>
    private void StartCountdown()
    {
        if (IsServer)
        {
            _countdownTimer.Value = 10f; // 10 seconds countdown
            _countdownActive.Value = true;
        }
    }
    
    /// <summary>
    /// Cancel the countdown if needed.
    /// </summary>
    public void CancelCountdown()
    {
        if (IsServer)
        {
            _countdownActive.Value = false;
        }
    }
    
    /// <summary>
    /// Get player info list
    /// </summary>
    public List<PlayerInfoNetworkData> GetPlayerInfos()
    {
        // Create a new list with all the network list items
        List<PlayerInfoNetworkData> list = new List<PlayerInfoNetworkData>();
        foreach (var item in _playerInfoList)
        {
            list.Add(item);
        }
        return list;
    }
    
    // Public properties
    public bool IsCountdownActive => _countdownActive.Value;
    public float CountdownTime => _countdownTimer.Value;
    public int PlayersReady => _playersReady.Value;
    public int TotalPlayers => _totalPlayers.Value;
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // Unsubscribe from events
        _playerInfoList.OnListChanged -= OnPlayerInfoListChanged;
        
        // Unregister from service locator
        GameServices.Unregister<NetworkLobbyManager>();
    }
}
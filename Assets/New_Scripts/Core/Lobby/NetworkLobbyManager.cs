using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using Core.GameManagement;
using System.Linq;

public class NetworkLobbyManager : NetworkBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private float readyCountdownDuration = 5f;
    [SerializeField] private string mainMenuSceneName = "LobbyScene"; // Customize this to your main menu scene

    // Network variables for player management
    private readonly NetworkVariable<float> countdownTimer = new NetworkVariable<float>();
    private readonly NetworkList<PlayerInfo> players;
    
    // Events
    public event Action<int, int> OnReadyStatusChanged;
    public event Action<float> OnCountdownTick;
    public event Action OnCountdownComplete;
    public event Action OnPlayerListChanged;
    
    // This event is for internal use only within the NetworkLobbyManager
    private event Action GameSceneLoaded;
    
    // State
    private bool countdownActive = false;
    private bool playerSpawningEnabled = false;
    private bool isShuttingDown = false; // New flag to prevent multiple shutdown attempts

    // Constructor to initialize NetworkList
    public NetworkLobbyManager()
    {
        players = new NetworkList<PlayerInfo>();
    }

    // Player data structure - must be a struct with only value types
    public struct PlayerInfo : INetworkSerializable, IEquatable<PlayerInfo>
    {
        public ulong ClientId;
        // Using int for name index instead of string
        public int NameIndex;
        public bool IsReady;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref NameIndex);
            serializer.SerializeValue(ref IsReady);
        }

        public bool Equals(PlayerInfo other)
        {
            return ClientId == other.ClientId;
        }
    }

    // Create a helper class for UI to use
    public class PlayerDisplayInfo
    {
        public ulong ClientId;
        public string PlayerName;
        public bool IsReady;
    }

    // Local dictionary to store player names (not networked)
    private Dictionary<ulong, string> playerNames = new Dictionary<ulong, string>();

    private void Awake()
    {
        // Apply DontDestroyOnLoad
        DontDestroyOnLoad(gameObject);
        
        // Reset shutdown flag
        isShuttingDown = false;
    }

    public override void OnNetworkSpawn()
    {
        // Register with service locator
        GameServices.Register<NetworkLobbyManager>(this);
        
        // Subscribe to network variable changes
        players.OnListChanged += HandlePlayerListChanged;
        countdownTimer.OnValueChanged += HandleCountdownTimerChanged;
        
        // Subscribe to scene events
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        
        Debug.Log("NetworkLobbyManager initialized");
    }
    
    // Handle scene load completion
    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        // Check if this is the game scene
        if (sceneName == gameSceneName)
        {
            Debug.Log("Game scene loaded - enabling player spawning");
            playerSpawningEnabled = true;
            GameSceneLoaded?.Invoke();
        }
    }
    
    // Public method to register for game scene loaded notifications
    public void RegisterGameSceneLoadedCallback(Action callback)
    {
        if (callback != null)
        {
            GameSceneLoaded += callback;
        }
    }
    
    // Public method to unregister from game scene loaded notifications
    public void UnregisterGameSceneLoadedCallback(Action callback)
    {
        if (callback != null)
        {
            GameSceneLoaded -= callback;
        }
    }
    
    // Called when a player connects
    public void PlayerConnected(ulong clientId)
    {
        if (!IsServer) return;
        
        // Store default name locally
        string defaultName = $"Player {clientId}";
        playerNames[clientId] = defaultName;
        
        // Add player to networked list (with only value types)
        var playerInfo = new PlayerInfo
        {
            ClientId = clientId,
            NameIndex = (int)clientId, // Just use ID as name index
            IsReady = false
        };
        
        players.Add(playerInfo);
        
        Debug.Log($"Player added to lobby: {clientId}");
    }
    
    // Called when a player disconnects
    public void PlayerDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        
        // Remove player from list
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                players.RemoveAt(i);
                Debug.Log($"Player removed from lobby: {clientId}");
                break;
            }
        }
        
        // Remove from names dictionary
        if (playerNames.ContainsKey(clientId))
        {
            playerNames.Remove(clientId);
        }
        
        // If countdown is active, check if we should cancel it
        CheckAndUpdateCountdown();
    }
    
    // Set player name (called by client, executed on server)
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerNameServerRpc(ulong clientId, string playerName)
    {
        // Store name in dictionary
        playerNames[clientId] = playerName;
        
        // No need to update networked list since we're tracking names separately
        Debug.Log($"Player {clientId} renamed to {playerName}");
        
        // Notify all clients of the player list change
        OnPlayerListChanged?.Invoke();
    }
    
    // Toggle player ready status (called by client, executed on server)
    [ServerRpc(RequireOwnership = false)]
    public void TogglePlayerReadyServerRpc(ulong clientId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                var playerInfo = players[i];
                playerInfo.IsReady = !playerInfo.IsReady;
                players[i] = playerInfo;
                
                Debug.Log($"Player {clientId} ready status toggled to {playerInfo.IsReady}");
                break;
            }
        }
        
        // Check if we should start/stop the countdown
        CheckAndUpdateCountdown();
    }
    
    // Update the countdown based on player ready status
    private void CheckAndUpdateCountdown()
    {
        if (!IsServer) return;
        
        int readyCount = 0;
        int totalPlayers = players.Count;
        
        // Count ready players
        foreach (var player in players)
        {
            if (player.IsReady) readyCount++;
        }
        
        // Broadcast ready status
        NotifyReadyStatusChanged(readyCount, totalPlayers);
        
        // Check if all players are ready
        bool allReady = readyCount > 0 && readyCount == totalPlayers;
        
        // Start or stop countdown
        if (allReady && !countdownActive)
        {
            // Start countdown
            countdownActive = true;
            countdownTimer.Value = readyCountdownDuration;
            Debug.Log("All players ready, countdown started");
        }
        else if (!allReady && countdownActive)
        {
            // Cancel countdown
            countdownActive = false;
            countdownTimer.Value = 0;
            Debug.Log("Countdown cancelled - not all players ready");
        }
    }
    
    // Update countdown timer
    private void Update()
    {
        if (IsServer && countdownActive)
        {
            countdownTimer.Value -= Time.deltaTime;
            if (countdownTimer.Value <= 0)
            {
                countdownActive = false;
                HandleCountdownComplete();
            }
        }
    }
    
    // Handle timer change (for clients)
    private void HandleCountdownTimerChanged(float oldValue, float newValue)
    {
        OnCountdownTick?.Invoke(newValue);
    }
    
    // Handle player list changes
    private void HandlePlayerListChanged(NetworkListEvent<PlayerInfo> changeEvent)
    {
        OnPlayerListChanged?.Invoke();
    }
    
    // Notify listeners about ready status
    private void NotifyReadyStatusChanged(int readyCount, int totalPlayers)
    {
        OnReadyStatusChanged?.Invoke(readyCount, totalPlayers);
    }
    
    // Handle countdown complete
    private void HandleCountdownComplete()
    {
        OnCountdownComplete?.Invoke();
        StartGame();
    }
    
    // Start the game by loading the game scene
    public void StartGame()
    {
        if (!IsServer) return;
        
        Debug.Log("Starting game - loading game scene");
        
        // Reset spawn flag - players will be spawned only after the game scene loads
        playerSpawningEnabled = false;
        
        // Load the game scene for all clients
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
    
    // Get player infos for UI
    public List<PlayerDisplayInfo> GetPlayerInfos()
    {
        List<PlayerDisplayInfo> result = new List<PlayerDisplayInfo>();
        
        foreach (var player in players)
        {
            string name = "Unknown Player";
            if (playerNames.TryGetValue(player.ClientId, out string storedName))
            {
                name = storedName;
            }
            
            result.Add(new PlayerDisplayInfo
            {
                ClientId = player.ClientId,
                PlayerName = name,
                IsReady = player.IsReady
            });
        }
        
        return result;
    }
    
    // Check if player spawning is currently enabled
    public bool IsPlayerSpawningEnabled()
    {
        return playerSpawningEnabled;
    }
    
    public void SpawnPlayersInGameScene()
    {
        // This method will be empty - it's just for the callback
        // The actual spawning is handled by GameInitializer
        Debug.Log("NetworkLobbyManager: Game scene loaded event triggered");
    }

    // Get the player data for spawning
    public List<ulong> GetConnectedClientIds()
    {
        List<ulong> clientIds = new List<ulong>();
        foreach (var player in players)
        {
            clientIds.Add(player.ClientId);
        }
        return clientIds;
    }
    
    // Get player name by client ID
    public string GetPlayerName(ulong clientId)
    {
        if (playerNames.TryGetValue(clientId, out string name))
        {
            return name;
        }
        return $"Player {clientId}";
    }
    
    // Clean disconnect and reset game functionality
    public void DisconnectAndResetGame()
    {
        // Prevent multiple shutdown attempts
        if (isShuttingDown)
        {
            Debug.Log("[NetworkLobbyManager] Already in shutdown process, ignoring additional request");
            return;
        }
        
        isShuttingDown = true;
        
        if (NetworkManager.Singleton == null) 
        {
            Debug.LogError("[NetworkLobbyManager] NetworkManager is null, cannot disconnect properly");
            // Try to load scene directly as fallback
            try {
                UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
            }
            catch (System.Exception e) {
                Debug.LogError($"[NetworkLobbyManager] Failed to load scene: {e.Message}");
                isShuttingDown = false; // Reset flag on error
            }
            return;
        }
        
        Debug.Log("[NetworkLobbyManager] Disconnecting and resetting game");
        
        // Check if we're the server - if so, tell all clients to disconnect FIRST
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[NetworkLobbyManager] Server shutting down network session");
            
            // Tell all clients to disconnect
            ReturnToMainMenuClientRpc();
            
            // Wait a moment to ensure clients receive the message
            StartCoroutine(ServerShutdownSequence());
        }
        else
        {
            // Just disconnect this client
            StartCoroutine(DisconnectAndLoadScene(mainMenuSceneName));
        }
    }
    
    // Proper shutdown sequence for server
    private IEnumerator ServerShutdownSequence()
    {
        Debug.Log("[NetworkLobbyManager] Server beginning shutdown sequence");
        
        // Give clients time to process the disconnect message
        yield return new WaitForSeconds(0.5f);
        
        // Force disconnect all clients
        if (NetworkManager.Singleton != null)
        {
            ulong[] connectedClients = NetworkManager.Singleton.ConnectedClientsIds.ToArray();
            foreach (ulong clientId in connectedClients)
            {
                if (clientId != NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log($"[NetworkLobbyManager] Server disconnecting client {clientId}");
                    NetworkManager.Singleton.DisconnectClient(clientId);
                }
            }
        }
        
        // Give a moment for client disconnections to process
        yield return new WaitForSeconds(0.5f);
        
        // Now shut down the server and load the lobby scene
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("[NetworkLobbyManager] Server shutting down");
            
            // Store the scene name locally since we're shutting down networking
            string sceneToLoad = mainMenuSceneName;
            
            // Shutdown the network
            NetworkManager.Singleton.Shutdown();
            
            // Wait a moment for shutdown to complete
            yield return new WaitForSeconds(0.5f);
            
            // Load the lobby scene
            Debug.Log($"[NetworkLobbyManager] Server loading scene: {sceneToLoad}");
            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NetworkLobbyManager] Server error loading scene: {e.Message}");
                
                // Last resort - try again with a direct call
                try
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0); // Load first scene
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NetworkLobbyManager] Final attempt failed: {ex.Message}");
                }
            }
        }
        
        // Reset shutdown flag
        isShuttingDown = false;
    }

    // Send the disconnect command to all clients
    [ClientRpc]
    private void ReturnToMainMenuClientRpc()
    {
        Debug.Log("[NetworkLobbyManager] Client received disconnect request from server");
        
        // Only handle this if we're a client, not the server
        if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsClient)
        {
            StartCoroutine(DisconnectAndLoadScene(mainMenuSceneName));
        }
    }

    // Coroutine to properly disconnect network and load scene for clients
    private IEnumerator DisconnectAndLoadScene(string sceneName)
    {
        Debug.Log($"[NetworkLobbyManager] Client disconnecting and loading {sceneName}");
        
        // Store scene name locally before disconnecting
        string sceneToLoad = sceneName;
        
        // Disconnect from the network
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[NetworkLobbyManager] Network shutdown completed");
        }
        
        // Wait a moment for disconnect to complete
        yield return new WaitForSeconds(0.5f);
        
        // Load the main menu scene
        Debug.Log($"[NetworkLobbyManager] Loading scene: {sceneToLoad}");
        
        try {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
        }
        catch (System.Exception e) {
            Debug.LogError($"[NetworkLobbyManager] Error loading scene: {e.Message}");
            
            // Fallback to first scene
            try {
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
            catch (System.Exception ex) {
                Debug.LogError($"[NetworkLobbyManager] Final attempt failed: {ex.Message}");
            }
        }
        
        // Reset shutdown flag
        isShuttingDown = false;
    }
    
    // ServerRpc for clients to request returning to lobby
    [ServerRpc(RequireOwnership = false)]
    public void RequestReturnToLobbyServerRpc()
    {
        Debug.Log("[NetworkLobbyManager] Client requested return to lobby");
        DisconnectAndResetGame();
    }
    
    public override void OnNetworkDespawn()
    {
        // Unsubscribe from events
        players.OnListChanged -= HandlePlayerListChanged;
        countdownTimer.OnValueChanged -= HandleCountdownTimerChanged;
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }
        
        // Unregister from service locator
        GameServices.Unregister<NetworkLobbyManager>();
    }
}
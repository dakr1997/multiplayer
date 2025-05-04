using UnityEngine;
using Unity.Netcode;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject _connectionUIPanel;
    [SerializeField] private GameObject _lobbyUIPanel;
    [SerializeField] private GameObject _gameStateManagerPrefab;
    [SerializeField] private GameObject _networkLobbyManagerPrefab;
    
    private void Awake()
    {
        Debug.Log("Game Bootstrap: Initializing core systems");
        
        // Show connection UI and hide lobby UI initially
        if (_connectionUIPanel != null)
        {
            _connectionUIPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Connection UI Panel reference is missing!");
        }
        
        if (_lobbyUIPanel != null)
        {
            _lobbyUIPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Lobby UI Panel reference is missing!");
        }
        
        // Check prefabs
        if (_gameStateManagerPrefab == null)
        {
            Debug.LogError("GameStateManager prefab is missing!");
        }
        
        if (_networkLobbyManagerPrefab == null)
        {
            Debug.LogError("NetworkLobbyManager prefab is missing!");
        }
    }
    
    private void Start()
    {
        // Subscribe to network events if NetworkManager exists
        if (NetworkManager.Singleton != null)
        {
            SubscribeToNetworkEvents();
        }
        else
        {
            Debug.LogWarning("NetworkManager not available yet. Waiting...");
            // Try again after a short delay
            Invoke("TrySubscribeToNetworkEvents", 0.5f);
        }
    }
    
    private void TrySubscribeToNetworkEvents()
    {
        if (NetworkManager.Singleton != null)
        {
            SubscribeToNetworkEvents();
        }
        else
        {
            Debug.LogError("NetworkManager still not available after delay!");
        }
    }
    
    private void SubscribeToNetworkEvents()
    {
        NetworkManager.Singleton.OnServerStarted += OnNetworkServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnNetworkServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
    
    private void OnNetworkServerStarted()
    {
        Debug.Log("Server started, spawning network objects");
        
        // Only spawn objects if we're the server
        if (NetworkManager.Singleton.IsServer)
        {
            // Spawn GameStateManager
            if (_gameStateManagerPrefab != null)
            {
                GameObject gameStateObj = Instantiate(_gameStateManagerPrefab);
                NetworkObject networkObject = gameStateObj.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Spawn();
                    Debug.Log("GameStateManager spawned successfully");
                }
                else
                {
                    Debug.LogError("GameStateManager prefab is missing NetworkObject component!");
                }
            }
            
            // Spawn NetworkLobbyManager
            if (_networkLobbyManagerPrefab != null)
            {
                GameObject lobbyManagerObj = Instantiate(_networkLobbyManagerPrefab);
                NetworkObject networkObject = lobbyManagerObj.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Spawn();
                    Debug.Log("NetworkLobbyManager spawned successfully");
                }
                else
                {
                    Debug.LogError("NetworkLobbyManager prefab is missing NetworkObject component!");
                }
            }
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"GameBootstrap: Client connected: {clientId}");
        
        // If we're connecting as a client (not host), hide connection UI
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
        {
            if (_connectionUIPanel != null)
            {
                _connectionUIPanel.SetActive(false);
            }
        }
    }
}
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;
using Core.GameManagement;

public class JoinMenu : MonoBehaviour
{
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private GameObject _lobbyPanel; // Reference to the lobby panel
    [SerializeField] private TMP_InputField _ipInputField;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    [SerializeField] private Button _serverButton;
    [SerializeField] private TextMeshProUGUI _statusText;
    
    private string _serverIP = "127.0.0.1"; // Default to localhost
    private bool _isConnected = false;
    private float _connectionCheckTimer = 0f;
    
    private void Awake()
    {
        // Check if NetworkManager exists first
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("NetworkManager not found in scene! Make sure it's in the scene.");
        }
        
        // Setup IP input field
        if (_ipInputField != null)
        {
            _ipInputField.text = _serverIP;
            _ipInputField.onEndEdit.AddListener(UpdateIPAddress);
        }
        
        // Setup buttons
        if (_hostButton) _hostButton.onClick.AddListener(OnHostButtonClicked);
        if (_clientButton) _clientButton.onClick.AddListener(OnClientButtonClicked);
        if (_serverButton) _serverButton.onClick.AddListener(OnServerButtonClicked);
        
        // Only subscribe to events if NetworkManager exists
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
            // Try again later
            Invoke("TrySubscribeToNetworkEvents", 0.5f);
        }
        
        // Make sure the lobby panel is initially hidden
        if (_lobbyPanel != null)
        {
            _lobbyPanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Check for successful client connection
        if (!_isConnected && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            _connectionCheckTimer += Time.deltaTime;
            
            // If we've been connected for a bit without seeing the callback, force show lobby
            if (_connectionCheckTimer > 3.0f && NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("Connection detected, showing lobby UI");
                ShowLobbyUI();
                _isConnected = true;
            }
        }
    }
    
    private void TrySubscribeToNetworkEvents()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
            Debug.LogError("NetworkManager still not available!");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        // Remove input field listener
        if (_ipInputField != null)
        {
            _ipInputField.onEndEdit.RemoveListener(UpdateIPAddress);
        }
    }
    
    private void UpdateIPAddress(string newIP)
    {
        _serverIP = newIP;
    }
    
    private void OnHostButtonClicked()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("Cannot start host: NetworkManager not found!");
            return;
        }
        
        SetTransportIP();
        NetworkManager.Singleton.StartHost();
        HideMenu();
        ShowLobbyUI();
        _isConnected = true;
    }
    
    private void OnClientButtonClicked()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("Cannot start client: NetworkManager not found!");
            return;
        }
        
        SetTransportIP();
        NetworkManager.Singleton.StartClient();
        HideMenu();
        
        // Reset connection timer - we'll show lobby UI when connection is confirmed
        _connectionCheckTimer = 0f;
        _isConnected = false;
    }
    
    private void OnServerButtonClicked()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("Cannot start server: NetworkManager not found!");
            return;
        }
        
        SetTransportIP();
        NetworkManager.Singleton.StartServer();
        HideMenu();
        ShowLobbyUI();
        _isConnected = true;
    }
    
    private void SetTransportIP()
    {
        if (NetworkManager.Singleton == null) return;
        
        // For Unity Transport
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = _serverIP;
            Debug.Log($"Set transport IP to: {_serverIP}");
        }
        else
        {
            Debug.LogError("UnityTransport component not found on NetworkManager!");
        }
        
        // Update status
        UpdateStatus();
    }
    
    private void HideMenu()
    {
        if (_menuPanel) _menuPanel.SetActive(false);
    }
    
    private void ShowMenu()
    {
        if (_menuPanel) _menuPanel.SetActive(true);
    }
    
    private void ShowLobbyUI()
    {
        // Try to find lobby panel if not assigned
        if (_lobbyPanel == null)
        {
            _lobbyPanel = GameObject.Find("LobbyPanel");
            
            if (_lobbyPanel == null)
            {
                // Look for it in specific parent canvases
                Transform parentCanvas = GameObject.Find("LoginCanvas")?.transform;
                if (parentCanvas != null)
                {
                    _lobbyPanel = parentCanvas.Find("LobbyPanel")?.gameObject;
                }
            }
        }
        
        // Activate lobby panel
        if (_lobbyPanel != null)
        {
            _lobbyPanel.SetActive(true);
            Debug.Log("Lobby panel activated");
        }
        else
        {
            Debug.LogError("Could not find LobbyPanel to activate!");
        }
    }
    
    private void UpdateStatus()
    {
        if (_statusText == null) return;
        if (NetworkManager.Singleton == null) return;
        
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";
        
        _statusText.text = $"Mode: {mode}\nConnected to: {_serverIP}";
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
        
        // If this is the local client that connected
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Local client connected, showing lobby UI");
            ShowLobbyUI();
            _isConnected = true;
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");
        
        // Notify the lobby manager about the disconnected player
        var lobbyManager = GameServices.Get<NetworkLobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.PlayerDisconnected(clientId);
        }
        
        // If this is the local client, show connection menu again
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Local client disconnected, showing connection menu");
            if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
            ShowMenu();
            _isConnected = false;
        }
    }
}
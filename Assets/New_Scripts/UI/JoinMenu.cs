using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;
using Core.GameManagement;
public class JoinMenu : MonoBehaviour
{
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private TMP_InputField _ipInputField;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    [SerializeField] private Button _serverButton;
    [SerializeField] private TextMeshProUGUI _statusText;
    
    private string _serverIP = "127.0.0.1"; // Default to localhost
    
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
    }
}
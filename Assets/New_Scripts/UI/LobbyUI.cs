using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;
using Core.GameManagement;
/// <summary>
/// UI for the lobby state.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    // Static reference for easier access
    private static LobbyUI _instance;
    
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private Button _readyButton;
    [SerializeField] private TextMeshProUGUI _readyStatusText;
    [SerializeField] private TextMeshProUGUI _countdownText;
    
    // Player list display components
    [SerializeField] private GameObject _playerEntryPrefab;
    [SerializeField] private Transform _playerEntriesParent;
    [SerializeField] private TMP_InputField _playerNameInput;
    
    // Reference to lobby manager
    private NetworkLobbyManager _lobbyManager;
    private List<GameObject> _playerEntries = new List<GameObject>();
    private bool _isReady = false;
    
    private void Awake()
    {
        _instance = this;
        
        // Hide UI initially
        if (_lobbyPanel) _lobbyPanel.SetActive(false);
        if (_countdownText) _countdownText.gameObject.SetActive(false);
        
        // Setup button
        if (_readyButton)
        {
            _readyButton.onClick.AddListener(OnReadyButtonClicked);
        }
    }
    
    private void Start()
    {
        // Try to get lobby manager, but don't error if not found yet
        TryGetLobbyManager();
        
        // Set up player name input
        if (_playerNameInput != null)
        {
            // Set default player name
            _playerNameInput.text = $"Player {Random.Range(1000, 9999)}";
            
            // Subscribe to name changes
            _playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
        }
    }

    private void TryGetLobbyManager()
    {
        _lobbyManager = GameServices.Get<NetworkLobbyManager>();
        
        if (_lobbyManager != null)
        {
            // Subscribe to events
            _lobbyManager.OnReadyStatusChanged += UpdateReadyStatus;
            _lobbyManager.OnCountdownTick += UpdateCountdown;
            _lobbyManager.OnPlayerListChanged += UpdatePlayerList;
            
            // Initial player list update
            UpdatePlayerList();
            
            Debug.Log("LobbyUI: Successfully connected to NetworkLobbyManager");
        }
        else
        {
            // Retry after a short delay
            Debug.LogWarning("LobbyUI: NetworkLobbyManager not found, will retry...");
            Invoke("TryGetLobbyManager", 0.5f);
        }
    }
    
    private void OnDestroy()
    {
        _instance = null;
        
        if (_lobbyManager != null)
        {
            // Unsubscribe from events
            _lobbyManager.OnReadyStatusChanged -= UpdateReadyStatus;
            _lobbyManager.OnCountdownTick -= UpdateCountdown;
            _lobbyManager.OnPlayerListChanged -= UpdatePlayerList;
        }
        
        if (_playerNameInput != null)
        {
            _playerNameInput.onEndEdit.RemoveListener(OnPlayerNameChanged);
        }
    }
    
    /// <summary>
    /// Called when the ready button is clicked.
    /// </summary>
    private void OnReadyButtonClicked()
    {
        if (_lobbyManager != null && NetworkManager.Singleton.IsClient)
        {
            // Toggle ready status
            _lobbyManager.TogglePlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
            
            // Update button text
            _isReady = !_isReady;
            if (_readyButton != null && _readyButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                _readyButton.GetComponentInChildren<TextMeshProUGUI>().text = _isReady ? "Cancel Ready" : "Ready";
            }
        }
    }
    
    /// <summary>
    /// Update the ready status text.
    /// </summary>
    private void UpdateReadyStatus(int playersReady, int totalPlayers)
    {
        if (_readyStatusText)
        {
            _readyStatusText.text = $"Players Ready: {playersReady}/{totalPlayers}";
        }
    }
    
    /// <summary>
    /// Update the countdown text.
    /// </summary>
    private void UpdateCountdown(float remainingTime)
    {
        if (_countdownText)
        {
            _countdownText.gameObject.SetActive(true);
            _countdownText.text = $"Game Starting in: {Mathf.CeilToInt(remainingTime)}";
        }
    }
    
    /// <summary>
    /// Update the player list UI.
    /// </summary>
    private void UpdatePlayerList()
    {
        // Additional safety checks
        if (_lobbyManager == null)
        {
            Debug.LogWarning("[LobbyUI] Cannot update player list: _lobbyManager is null");
            return;
        }
        
        if (_playerEntryPrefab == null)
        {
            Debug.LogError("[LobbyUI] Cannot update player list: _playerEntryPrefab is null");
            return;
        }
        
        if (_playerEntriesParent == null)
        {
            Debug.LogError("[LobbyUI] Cannot update player list: _playerEntriesParent is null");
            return;
        }
        
        // Clear existing entries - add safety check for each entry
        foreach (var entry in _playerEntries)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }
        _playerEntries.Clear();
        
        // Get player info list from lobby manager with safe handling
        List<NetworkLobbyManager.PlayerDisplayInfo> players;
        try
        {
            players = _lobbyManager.GetPlayerInfos();
            if (players == null)
            {
                Debug.LogWarning("[LobbyUI] GetPlayerInfos returned null");
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LobbyUI] Error getting player infos: {e.Message}");
            return;
        }
        
        // Create entry for each player
        foreach (var player in players)
        {
            try
            {
                GameObject entryObj = Instantiate(_playerEntryPrefab, _playerEntriesParent);
                if (entryObj == null)
                {
                    Debug.LogError("[LobbyUI] Failed to instantiate player entry prefab");
                    continue;
                }
                
                // Set player name with null checks
                Transform nameTransform = entryObj.transform.Find("NameText");
                if (nameTransform != null)
                {
                    TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = player.PlayerName;
                    }
                    else
                    {
                        Debug.LogWarning("[LobbyUI] TextMeshProUGUI component not found on NameText");
                    }
                }
                else
                {
                    Debug.LogWarning("[LobbyUI] NameText not found in player entry prefab");
                }
                
                // Set ready status with null checks
                Transform statusTransform = entryObj.transform.Find("StatusText");
                if (statusTransform != null)
                {
                    TextMeshProUGUI statusText = statusTransform.GetComponent<TextMeshProUGUI>();
                    if (statusText != null)
                    {
                        bool isReady = player.IsReady;
                        statusText.text = isReady ? "Ready" : "Not Ready";
                        statusText.color = isReady ? Color.green : Color.red;
                    }
                    else
                    {
                        Debug.LogWarning("[LobbyUI] TextMeshProUGUI component not found on StatusText");
                    }
                }
                else
                {
                    Debug.LogWarning("[LobbyUI] StatusText not found in player entry prefab");
                }
                
                // Highlight local player with null checks
                if (NetworkManager.Singleton != null && player.ClientId == NetworkManager.Singleton.LocalClientId)
                {
                    Image background = entryObj.GetComponent<Image>();
                    if (background != null)
                    {
                        background.color = new Color(0.8f, 0.9f, 1f, 0.7f); // Light blue highlight
                    }
                }
                
                _playerEntries.Add(entryObj);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyUI] Error creating player entry: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Called when player name is changed.
    /// </summary>
    private void OnPlayerNameChanged(string newName)
    {
        if (_lobbyManager != null && NetworkManager.Singleton.IsClient)
        {
            // Update player name
            _lobbyManager.SetPlayerNameServerRpc(NetworkManager.Singleton.LocalClientId, newName);
        }
    }
    
    /// <summary>
    /// Show the lobby UI.
    /// </summary>
    public static void Show()
    {
        if (_instance != null && _instance._lobbyPanel != null)
        {
            _instance._lobbyPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide the lobby UI.
    /// </summary>
    public static void Hide()
    {
        if (_instance != null && _instance._lobbyPanel != null)
        {
            _instance._lobbyPanel.SetActive(false);
        }
    }
}
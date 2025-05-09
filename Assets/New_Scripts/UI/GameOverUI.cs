using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.GameState;
using Core.GameManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button returnToLobbyButton;
    
    private void Awake()
    {
        Debug.Log("[GameOverUI] Initializing");
        
        // Connect button click handler
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(ReturnToLobby);
            Debug.Log("[GameOverUI] Return to Lobby button connected");
        }
        else
        {
            Debug.LogError("[GameOverUI] ReturnToLobbyButton reference is missing!");
            
            // Try to find button if not assigned
            returnToLobbyButton = GetComponentInChildren<Button>();
            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.onClick.AddListener(ReturnToLobby);
                Debug.Log("[GameOverUI] Found ReturnToLobbyButton automatically");
            }
        }
    }
    
    public void SetResult(bool victory)
    {
        if (statusText != null)
        {
            statusText.text = victory ? "VICTORY!" : "DEFEAT!";
            statusText.color = victory ? Color.green : Color.red;
            Debug.Log($"[GameOverUI] Result set to {statusText.text}");
        }
        else
        {
            Debug.LogError("[GameOverUI] StatusText reference is missing!");
        }
    }
    
    public void Show(bool victory)
    {
        gameObject.SetActive(true);
        SetResult(victory);
        Debug.Log($"[GameOverUI] Showing with {(victory ? "Victory" : "Defeat")}");
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    private void ReturnToLobby()
    {
        Debug.Log("[GameOverUI] ReturnToLobby button clicked");
        
        // Get GameManager through service locator - going directly to GameManager now
        GameManager gameManager = GameServices.Get<GameManager>();
        if (gameManager != null)
        {
            Debug.Log("[GameOverUI] Found GameManager, requesting return to lobby");
            gameManager.ReturnToLobby();
            return;
        }
        
        Debug.LogError("[GameOverUI] Could not find GameManager!");
        
        // Fallbacks if GameManager isn't found
        NetworkLobbyManager lobbyManager = GameServices.Get<NetworkLobbyManager>();
        if (lobbyManager != null)
        {
            Debug.Log("[GameOverUI] Found NetworkLobbyManager, calling DisconnectAndResetGame");
            lobbyManager.DisconnectAndResetGame();
            return;
        }
        
        // Last resort - try to directly load the scene
        Debug.LogError("[GameOverUI] No managers found, trying direct scene load");
        try {
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
            Debug.Log("[GameOverUI] Directly loaded LobbyScene");
        }
        catch (System.Exception e) {
            Debug.LogError($"[GameOverUI] Failed to load scene: {e.Message}");
        }
    }
}
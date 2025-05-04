// File: Assets/_Project/Scripts/Core/GameState/LobbyState.cs
using UnityEngine;

/// <summary>
/// Game state for pre-game lobby.
/// </summary>
public class LobbyState : GameState
{
    private NetworkLobbyManager _lobbyManager;
    
    public LobbyState(GameStateManager stateManager) : base(stateManager)
    {
    }
    
    public override void Enter()
    {
        Debug.Log("Entering Lobby State");
        
        // Get the network lobby manager
        _lobbyManager = GameServices.Get<NetworkLobbyManager>();
        
        // Enable lobby UI
        LobbyUI.Show();
        
        // Subscribe to lobby manager events
        if (_lobbyManager != null)
        {
            _lobbyManager.OnCountdownComplete += OnCountdownComplete;
        }
    }
    
    public override void Exit()
    {
        Debug.Log("Exiting Lobby State");
        
        // Hide lobby UI
        LobbyUI.Hide();
        
        // Unsubscribe from events
        if (_lobbyManager != null)
        {
            _lobbyManager.OnCountdownComplete -= OnCountdownComplete;
        }
    }
    
    public override void Update()
    {
        // State is managed by NetworkLobbyManager
    }
    
    /// <summary>
    /// Called when the countdown completes
    /// </summary>
    private void OnCountdownComplete()
    {
        // Transition to wave state handled by NetworkLobbyManager
    }
}
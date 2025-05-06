using UnityEngine;
using Core.GameManagement;

namespace Core.GameState
{
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
            
            // Show lobby UI if it exists
            ShowLobbyUI();
            
            // Subscribe to lobby manager events
            if (_lobbyManager != null)
            {
                _lobbyManager.OnCountdownComplete += OnCountdownComplete;
            }
            else
            {
                Debug.LogWarning("NetworkLobbyManager not found when entering Lobby state!");
            }
        }
        
        private void ShowLobbyUI()
        {
            // Use try-catch in case we're in a scene without the LobbyUI
            try
            {
                LobbyUI.Show();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not show LobbyUI: {e.Message}");
            }
        }
        
        public override void Exit()
        {
            Debug.Log("Exiting Lobby State");
            
            // Hide lobby UI
            try
            {
                LobbyUI.Hide();
            }
            catch (System.Exception)
            {
                // Ignore errors when hiding UI - might be in different scene
            }
            
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
            Debug.Log("Lobby countdown complete - game starting");
            // Scene transition is handled by NetworkLobbyManager
        }
    }
}
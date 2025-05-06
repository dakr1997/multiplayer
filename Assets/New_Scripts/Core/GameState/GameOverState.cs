using UnityEngine;
using Core.GameManagement;

namespace Core.GameState
{
    /// <summary>
    /// Game state for game over (victory or defeat).
    /// </summary>
    public class GameOverState : GameState
    {
        private GameManager gameManager;
        private bool isVictory;
        
        public GameOverState(GameStateManager stateManager) : base(stateManager)
        {
        }
        
        public override void Enter()
        {
            Debug.Log("Entering GameOver State");
            
            // Find GameManager through service locator
            gameManager = GameServices.Get<GameManager>();
            
            if (gameManager != null)
            {
                // Check if this was a victory or defeat
                isVictory = gameManager.IsVictory();
                
                // Show appropriate UI
                ShowGameOverUI(isVictory);
            }
            else
            {
                Debug.LogWarning("GameManager not found when entering GameOver state!");
            }
        }
        
        public override void Exit()
        {
            Debug.Log("Exiting GameOver State");
            
            // Hide game over UI
            HideGameOverUI();
        }
        
        public override void Update()
        {
            // Game over state is typically static until a button is pressed
            // or some other input to restart
        }
        
        /// <summary>
        /// Set victory/defeat status - called by GameManager
        /// </summary>
        public void SetVictory(bool victory)
        {
            isVictory = victory;
        }
        
        /// <summary>
        /// Restart the game - typically called from UI button
        /// </summary>
        public void RestartGame()
        {
            if (gameManager != null)
            {
                // Ask GameManager to start a new game
                gameManager.StartGame();
            }
            else if (StateManager != null)
            {
                // Fallback to direct state change
                StateManager.ChangeState(GameStateType.Lobby);
            }
        }
        
        /// <summary>
        /// Show the game over UI with appropriate victory/defeat message
        /// </summary>
        private void ShowGameOverUI(bool victory)
        {
            // Implementation depends on your UI system
            Debug.Log($"Showing game over UI: {(victory ? "Victory!" : "Defeat!")}");
            
            // TODO: Implement UI activation based on your UI system
            // Example: GameOverPanel.Show(victory);
        }
        
        /// <summary>
        /// Hide the game over UI
        /// </summary>
        private void HideGameOverUI()
        {
            // Implementation depends on your UI system
            Debug.Log("Hiding game over UI");
            
            // TODO: Implement UI deactivation based on your UI system
            // Example: GameOverPanel.Hide();
        }
    }
}
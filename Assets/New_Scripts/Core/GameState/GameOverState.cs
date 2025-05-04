using UnityEngine;

/// <summary>
/// Game state for game over.
/// </summary>
public class GameOverState : GameState
{
    public GameOverState(GameStateManager stateManager) : base(stateManager)
    {
    }
    
    public override void Enter()
    {
        Debug.Log("Entering Game Over State");
        
        // Show game over UI
        // TODO: Show game over UI
    }
    
    public override void Exit()
    {
        Debug.Log("Exiting Game Over State");
        
        // Hide game over UI
        // TODO: Hide game over UI
    }
    
    public override void Update()
    {
        // Wait for player input to restart or return to lobby
    }
    
    /// <summary>
    /// Restart the game
    /// </summary>
    public void RestartGame()
    {
        // Implement restart logic
        StateManager.ChangeState(GameStateType.Lobby);
    }
}
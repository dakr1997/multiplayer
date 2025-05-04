using UnityEngine;
using System.Collections;

/// <summary>
/// Game state for between-wave building and upgrades.
/// </summary>
public class BuildingState : GameState
{
    private float _buildingTime = 30f; // Seconds for building phase
    private float _timer;
    private bool _timerActive = false;
    
    public BuildingState(GameStateManager stateManager) : base(stateManager)
    {
    }
    
    public override void Enter()
    {
        Debug.Log("Entering Building State");
        
        // Reset timer
        _timer = _buildingTime;
        _timerActive = true;
        
        // Enable building UI/functionality
        EnableBuildingMode(true);
    }
    
    public override void Exit()
    {
        Debug.Log("Exiting Building State");
        
        // Disable building UI/functionality
        EnableBuildingMode(false);
        
        // Reset timer
        _timerActive = false;
    }
    
    public override void Update()
    {
        if (_timerActive)
        {
            _timer -= Time.deltaTime;
            
            // Check if time is up
            if (_timer <= 0)
            {
                // Transition back to wave state
                StateManager.ChangeState(GameStateType.Wave);
            }
        }
    }
    
    private void EnableBuildingMode(bool enabled)
    {
        // TODO: Implement building mode activation
        // This would involve enabling UI panels, tower placement mode, etc.
    }
    
    /// <summary>
    /// Allow players to bypass the timer and start next wave
    /// </summary>
    public void SkipBuilding()
    {
        StateManager.ChangeState(GameStateType.Wave);
    }
}

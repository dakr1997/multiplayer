// File: Scripts/Core/GameState/GameState.cs
/// <summary>
/// Base class for all game states.
/// </summary>
public abstract class GameState
{
    protected GameStateManager StateManager;
    
    public GameState(GameStateManager stateManager)
    {
        StateManager = stateManager;
    }
    
    /// <summary>
    /// Called when entering this state.
    /// </summary>
    public abstract void Enter();
    
    /// <summary>
    /// Called when exiting this state.
    /// </summary>
    public abstract void Exit();
    
    /// <summary>
    /// Update logic for this state.
    /// </summary>
    public abstract void Update();
}

// File: Assets/_Project/Scripts/Core/GameState/WaveState.cs
using UnityEngine;

/// <summary>
/// Game state for wave-based combat.
/// </summary>
public class WaveState : GameState
{
    private WaveManager _waveManager;
    
    public WaveState(GameStateManager stateManager) : base(stateManager)
    {
    }
    
    public override void Enter()
    {
        Debug.Log("Entering Wave State");
        
        // Get wave manager from service locator
        _waveManager = GameServices.Get<WaveManager>();
        
        if (_waveManager != null)
        {
            // Start wave
            _waveManager.StartNextWave();
        }
        else
        {
            Debug.LogError("WaveManager not found in GameServices!");
        }
        
        // Enable enemy spawners
        EnableEnemySpawners(true);
    }
    
    public override void Exit()
    {
        Debug.Log("Exiting Wave State");
        
        // Disable enemy spawners
        EnableEnemySpawners(false);
    }
    
    public override void Update()
    {
        // Check if wave is complete
        if (_waveManager != null && _waveManager.IsCurrentWaveComplete)
        {
            // Transition to building state
            StateManager.ChangeState(GameStateType.Building);
        }
        
        // Check for game over condition (e.g., main tower destroyed)
        var mainTower = GameServices.Get<MainTowerHP>();
        if (mainTower != null && !mainTower.IsAlive)
        {
            StateManager.ChangeState(GameStateType.GameOver);
        }
    }
    
    private void EnableEnemySpawners(bool enabled)
    {
        // Find and enable/disable all enemy spawners
        var spawners = Object.FindObjectsOfType<EnemySpawner>();
        foreach (var spawner in spawners)
        {
            spawner.SetSpawningEnabled(enabled);
        }
    }
}
using UnityEngine;
using Core.GameManagement;
using Core.Enemies.Base;
using Core.Towers;
namespace Core.GameState
{
    /// <summary>
    /// Game state for the building phase between waves.
    /// </summary>
    public class BuildingState : GameState
    {
        private GameManager gameManager;
        private float remainingTime;
        
        public BuildingState(GameStateManager stateManager) : base(stateManager)
        {
        }
        
        public override void Enter()
        {
            Debug.Log("Entering Building State");
            
            // Find GameManager through service locator
            gameManager = GameServices.Get<GameManager>();
            
            if (gameManager != null)
            {
                // Subscribe to building phase timer updates
                gameManager.OnBuildingTimeUpdated += UpdateBuildingTime;
                gameManager.OnBuildingPhaseStarted += OnBuildingPhaseStarted;
            }
            else
            {
                Debug.LogWarning("GameManager not found when entering Building state!");
            }
            
            // Enable tower building UI/functionality
            EnableTowerBuilding(true);
            
            // Disable enemies if any are still active
            DisableEnemies();
        }
        
        private void OnBuildingPhaseStarted()
        {
            Debug.Log("Building phase started");
            
            // Additional initialization when building phase starts
            // This could include giving players resources, updating UI, etc.
        }
        
        private void UpdateBuildingTime(int seconds)
        {
            remainingTime = seconds;
            
            // Update any UI elements showing the countdown
            // This might be handled elsewhere (like in a UI controller)
        }
        
        public override void Exit()
        {
            Debug.Log("Exiting Building State");
            
            // Disable tower building
            EnableTowerBuilding(false);
            
            // Unsubscribe from events
            if (gameManager != null)
            {
                gameManager.OnBuildingTimeUpdated -= UpdateBuildingTime;
                gameManager.OnBuildingPhaseStarted -= OnBuildingPhaseStarted;
            }
        }
        
        public override void Update()
        {
            // Most logic is handled by the GameManager through events
            // This method could be used for local state-specific updates
        }
        
        /// <summary>
        /// Enable or disable tower building functionality
        /// </summary>
        private void EnableTowerBuilding(bool enabled)
        {
            // Find tower spawner(s)
            TowerSpawner[] towerSpawners = UnityEngine.Object.FindObjectsByType<TowerSpawner>(FindObjectsSortMode.None);
            
            foreach (var spawner in towerSpawners)
            {
                // Enable or disable based on parameter
                if (spawner != null)
                {
                    // Assuming TowerSpawner has a way to enable/disable
                    spawner.enabled = enabled;
                }
            }
        }
        
        /// <summary>
        /// Disable any active enemies when entering building state
        /// </summary>
        private void DisableEnemies()
        {
            // Find enemy spawners and disable them
            EnemySpawner[] enemySpawners = UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
            
            foreach (var spawner in enemySpawners)
            {
                if (spawner != null)
                {
                    spawner.SetSpawningEnabled(false);
                    spawner.OnWaveStateExited();
                }
            }
        }
        
        /// <summary>
        /// Skip the building phase and immediately start the next wave
        /// </summary>
        public void SkipBuilding()
        {
            if (StateManager != null)
            {
                Debug.Log("Skipping building phase");
                StateManager.ChangeState(GameStateType.Wave);
            }
        }
    }
}
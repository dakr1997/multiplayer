using UnityEngine;
using Core.GameManagement;
using System.Collections.Generic;
using System.Linq;
using Core.WaveSystem;
using Core.Enemies.Base;
namespace Core.GameState
{
    /// <summary>
    /// Game state for active combat waves.
    /// </summary>
    public class WaveState : GameState
    {
        private GameManager gameManager;
        private WaveManager waveManager;
        private List<EnemySpawner> enemySpawners = new List<EnemySpawner>();
        private int currentWave;
        
        public WaveState(GameStateManager stateManager) : base(stateManager)
        {
        }
        
        public override void Enter()
        {
            Debug.Log("Entering Wave State");
            
            // Find GameManager through service locator
            gameManager = GameServices.Get<GameManager>();
            
            if (gameManager != null)
            {
                // Get current wave number
                currentWave = gameManager.GetCurrentWave();
                
                // Subscribe to wave events
                gameManager.OnWaveStarted += OnWaveStarted;
                gameManager.OnWaveCompleted += OnWaveCompleted;
            }
            else
            {
                Debug.LogWarning("GameManager not found when entering Wave state!");
            }
            
            // Find WaveManager
            waveManager = GameServices.Get<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogWarning("WaveManager not found when entering Wave state!");
            }
            
            // Find and enable enemy spawners
            FindAndEnableEnemySpawners();
        }
        
        private void OnWaveStarted(int waveNumber)
        {
            Debug.Log($"Wave {waveNumber} started");
            currentWave = waveNumber;
            
            // Enable spawners for this wave
            foreach (var spawner in enemySpawners)
            {
                if (spawner != null)
                {
                    spawner.OnWaveStateEntered();
                }
            }
        }
        
        private void OnWaveCompleted(int waveNumber)
        {
            Debug.Log($"Wave {waveNumber} completed");
            
            // Additional wave completion logic if needed
        }
        
        public override void Exit()
        {
            Debug.Log("Exiting Wave State");
            
            // Disable enemy spawners
            foreach (var spawner in enemySpawners)
            {
                if (spawner != null)
                {
                    spawner.SetSpawningEnabled(false);
                    spawner.OnWaveStateExited();
                }
            }
            
            // Unsubscribe from events
            if (gameManager != null)
            {
                gameManager.OnWaveStarted -= OnWaveStarted;
                gameManager.OnWaveCompleted -= OnWaveCompleted;
            }
        }
        
        public override void Update()
        {
            // Check if all enemies are defeated
            if (IsWaveComplete())
            {
                // Notify WaveManager that the wave is complete
                if (waveManager != null)
                {
                    // This assumes WaveManager has the appropriate method
                    waveManager.CompleteCurrentWave();
                }
                
                // State change is handled by GameManager via events
            }
        }

        
        
        /// <summary>
        /// Find and enable all enemy spawners in the scene
        /// </summary>
        private void FindAndEnableEnemySpawners()
        {
            // Clear previous list
            enemySpawners.Clear();
            
            // Find all enemy spawners
            EnemySpawner[] spawners = UnityEngine.Object.FindObjectsOfType<EnemySpawner>();
            
            if (spawners.Length == 0)
            {
                Debug.LogWarning("No EnemySpawners found in scene!");
            }
            
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                {
                    enemySpawners.Add(spawner);
                    spawner.SetSpawningEnabled(true);
                }
            }
            
            Debug.Log($"Found {enemySpawners.Count} enemy spawners");
        }
        
        /// <summary>
        /// Check if the current wave is complete
        /// </summary>
        private bool IsWaveComplete()
        {
            // No wave is active if there are no spawners
            if (enemySpawners.Count == 0)
            {
                return false;
            }
            
            // Check if all spawners are done and no enemies remain
            bool allSpawnersDone = enemySpawners.All(spawner => 
                spawner == null || spawner.IsDoneSpawning());
                
            if (!allSpawnersDone)
            {
                return false;
            }
            
            // Check if any enemies remain alive
            int activeEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>().Length;
            return activeEnemies == 0;
        }
    }
}
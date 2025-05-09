using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using Core.GameManagement;
using Core.Enemies.Base;

namespace Core.WaveSystem
{
    /// <summary>
    /// Manages wave spawning and progression
    /// </summary>
    public class WaveManager : NetworkBehaviour
    {
        [Header("Wave Configuration")]
        [SerializeField] private int totalWaves = 5;
        [SerializeField] private float timeBetweenWaves = 30f;
        [SerializeField] private GameObject[] enemyPrefabs;
        
        [Header("Wave Difficulty")]
        [SerializeField] private float enemyHealthMultiplierPerWave = 0.2f;
        [SerializeField] private float enemyDamageMultiplierPerWave = 0.1f;
        [SerializeField] private int enemiesPerWave = 10;
        [SerializeField] private int additionalEnemiesPerWave = 5;
        
        // Network variables
        private NetworkVariable<int> currentWave = new NetworkVariable<int>(0);
        private NetworkVariable<bool> isWaveActive = new NetworkVariable<bool>(false);
        
        // Events
        // Static events for compatibility with GameManager
        public static event Action<int> OnWaveCompleted;
        public static event Action OnAllWavesCompleted;
        
        // Instance events
        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;
        public event Action AllWavesCompleted;
        
        /// <summary>
        /// Initializes the WaveManager
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Register with service locator
            GameServices.Register<WaveManager>(this);
            
            // Reset wave counter
            if (IsServer)
            {
                currentWave.Value = 0;
                isWaveActive.Value = false;
            }
            
            Debug.Log("[WaveManager] Initialized");
        }
        
        /// <summary>
        /// Start a new wave
        /// </summary>
        public void StartWave(int waveNumber)
        {
            if (!IsServer) return;
            
            // Update network variables
            currentWave.Value = waveNumber;
            isWaveActive.Value = true;
            
            Debug.Log($"[WaveManager] Starting wave {waveNumber}");
            
            // Calculate difficulty for this wave
            float healthMultiplier = 1f + (waveNumber - 1) * enemyHealthMultiplierPerWave;
            float damageMultiplier = 1f + (waveNumber - 1) * enemyDamageMultiplierPerWave;
            int enemyCount = enemiesPerWave + (waveNumber - 1) * additionalEnemiesPerWave;
            
            // Notify spawners about wave settings
            BroadcastWaveSettings(waveNumber, healthMultiplier, damageMultiplier, enemyCount);
            
            // Trigger wave started event
            WaveStarted?.Invoke(waveNumber);
        }
        
        /// <summary>
        /// Called by WaveState when all enemies are defeated
        /// </summary>
        public void CompleteCurrentWave()
        {
            if (!IsServer || !isWaveActive.Value) return;
            
            int completedWave = currentWave.Value;
            
            Debug.Log($"[WaveManager] Wave {completedWave} completed");
            
            // Update network variables
            isWaveActive.Value = false;
            
            // Trigger wave completed events
            WaveCompleted?.Invoke(completedWave);
            OnWaveCompleted?.Invoke(completedWave); // Static event for GameManager
            
            // Check if this was the last wave
            if (completedWave >= totalWaves)
            {
                Debug.Log("[WaveManager] All waves completed!");
                
                // Trigger all waves completed events
                AllWavesCompleted?.Invoke();
                OnAllWavesCompleted?.Invoke(); // Static event for GameManager
            }
        }
        
        /// <summary>
        /// Broadcast wave settings to all spawners
        /// </summary>
        private void BroadcastWaveSettings(int waveNumber, float healthMultiplier, float damageMultiplier, int enemyCount)
        {
            // Find all spawners
            EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
            
            if (spawners.Length == 0)
            {
                Debug.LogWarning("[WaveManager] No enemy spawners found!");
                return;
            }
            
            // Calculate enemies per spawner (distribute evenly)
            int baseEnemiesPerSpawner = enemyCount / spawners.Length;
            int remainingEnemies = enemyCount % spawners.Length;
            
            // Configure each spawner
            for (int i = 0; i < spawners.Length; i++)
            {
                // Calculate how many enemies this spawner should create
                int spawnerEnemyCount = baseEnemiesPerSpawner;
                if (i < remainingEnemies)
                {
                    spawnerEnemyCount++; // Distribute remaining enemies
                }
                
                // Set spawner wave settings
                if (spawners[i] != null)
                {
                    spawners[i].ConfigureForWave(waveNumber, spawnerEnemyCount, healthMultiplier, damageMultiplier);
                }
            }
        }
        
        /// <summary>
        /// Get the current wave number
        /// </summary>
        public int GetCurrentWave()
        {
            return currentWave.Value;
        }
        
        /// <summary>
        /// Check if a wave is currently active
        /// </summary>
        public bool IsWaveActive()
        {
            return isWaveActive.Value;
        }
        
        /// <summary>
        /// Get the total number of waves
        /// </summary>
        public int GetTotalWaves()
        {
            return totalWaves;
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            // Unregister from service locator
            GameServices.Unregister<WaveManager>();
        }
    }
}
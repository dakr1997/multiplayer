using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System;

public class WaveManager : NetworkBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave";
        public List<EnemySpawnConfig> enemies = new List<EnemySpawnConfig>();
        public float waveDelay = 5f;
    }
    
    [System.Serializable]
    public class EnemySpawnConfig
    {
        public GameObject enemyPrefab;
        public int count = 10;
        public float spawnInterval = 1f;
    }
    
    [Header("Wave Settings")]
    [SerializeField] private List<Wave> waves = new List<Wave>();
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float initialDelay = 3f;
    [SerializeField] private bool loopWaves = true;
    
    // Network variables
    private NetworkVariable<int> currentWaveIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<int> remainingEnemiesInWave = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    // State
    private int currentEnemyIndex = 0;
    private int enemiesSpawnedInWave = 0;
    private float nextSpawnTime = 0f;
    private bool waveInProgress = false;
    
    // Events
    public static event Action<int, string> OnWaveStarted;
    public static event Action<int> OnWaveCompleted;
    public static event Action OnAllWavesCompleted;
    
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        // Initialize
        currentWaveIndex.Value = 0;
        nextSpawnTime = Time.time + initialDelay;
        
        // Subscribe to enemy death
        EnemyAI.OnEnemyDied += HandleEnemyDeath;
        
        Debug.Log($"Wave manager initialized with {waves.Count} waves");
    }
    
    private void Update()
    {
        if (!IsServer) return;
        
        // Wait for spawn time
        if (Time.time < nextSpawnTime) return;
        
        // Handle wave logic
        if (!waveInProgress)
        {
            StartNextWave();
        }
        else
        {
            SpawnEnemiesInCurrentWave();
        }
    }
    
    private void StartNextWave()
    {
        if (waves.Count == 0)
        {
            Debug.LogWarning("No waves configured!");
            return;
        }
        
        // Check if we've gone through all waves
        if (currentWaveIndex.Value >= waves.Count)
        {
            if (loopWaves)
            {
                currentWaveIndex.Value = 0;
                Debug.Log("Looping back to first wave");
            }
            else
            {
                Debug.Log("All waves completed!");
                OnAllWavesCompleted?.Invoke();
                enabled = false;
                return;
            }
        }
        
        // Get current wave
        Wave wave = waves[currentWaveIndex.Value];
        
        // Initialize wave variables
        currentEnemyIndex = 0;
        enemiesSpawnedInWave = 0;
        waveInProgress = true;
        
        // Count total enemies
        int totalEnemies = 0;
        foreach (var enemy in wave.enemies)
        {
            totalEnemies += enemy.count;
        }
        
        remainingEnemiesInWave.Value = totalEnemies;
        
        // Set next spawn time
        nextSpawnTime = Time.time;
        
        // Announce wave start
        Debug.Log($"Starting Wave {currentWaveIndex.Value + 1}: {wave.waveName}");
        OnWaveStarted?.Invoke(currentWaveIndex.Value + 1, wave.waveName);
        
        // UI notification
        AnnounceWaveStartClientRpc(currentWaveIndex.Value + 1, wave.waveName);
    }
    
    private void SpawnEnemiesInCurrentWave()
    {
        if (currentWaveIndex.Value >= waves.Count) return;
        
        Wave wave = waves[currentWaveIndex.Value];
        
        // Check if we've spawned all enemies in this wave
        if (currentEnemyIndex >= wave.enemies.Count)
        {
            // Wave spawning complete
            waveInProgress = false;
            
            // Increment wave index
            currentWaveIndex.Value++;
            
            // Set delay for next wave
            nextSpawnTime = Time.time + wave.waveDelay;
            
            return;
        }
        
        // Get current enemy config
        EnemySpawnConfig config = wave.enemies[currentEnemyIndex];
        
        // Spawn enemy
        if (enemiesSpawnedInWave < config.count)
        {
            SpawnEnemy(config.enemyPrefab);
            enemiesSpawnedInWave++;
            
            // Set time for next spawn
            nextSpawnTime = Time.time + config.spawnInterval;
        }
        else
        {
            // Move to next enemy type
            currentEnemyIndex++;
            enemiesSpawnedInWave = 0;
        }
    }
    
    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points defined!");
            return;
        }
        
        // Choose random spawn point
        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        
        // Instantiate enemy
        GameObject enemy = Instantiate(
            enemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );
        
        // Spawn on network
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }
        else
        {
            Debug.LogError("Enemy prefab missing NetworkObject component!");
            Destroy(enemy);
        }
    }
    
    private void HandleEnemyDeath(EnemyAI enemy)
    {
        if (!IsServer) return;
        
        // Decrement enemy count
        remainingEnemiesInWave.Value--;
        
        // Check if wave is complete
        if (remainingEnemiesInWave.Value <= 0 && !waveInProgress)
        {
            // Wave completed
            Debug.Log($"Wave {currentWaveIndex.Value} completed!");
            OnWaveCompleted?.Invoke(currentWaveIndex.Value);
        }
    }
    
    [ClientRpc]
    private void AnnounceWaveStartClientRpc(int waveNumber, string waveName)
    {
        // UI notification
        Debug.Log($"Wave {waveNumber}: {waveName} started!");
    }
    
    public override void OnNetworkDespawn()
    {
        // Unsubscribe from events
        EnemyAI.OnEnemyDied -= HandleEnemyDeath;
    }
}
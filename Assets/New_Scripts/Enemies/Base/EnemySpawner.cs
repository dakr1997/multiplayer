// Location: Core/Enemies/Base/EnemySpawner.cs
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using Core.GameManagement;
using Core.WaveSystem;
using Core.Components;
using Core.Interfaces;

namespace Core.Enemies.Base
{
    /// <summary>
    /// Handles enemy spawning during wave state
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawning Settings")]
        [SerializeField] private NetworkObject[] enemyPrefabs;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private float initialDelay = 1f;
        
        [Header("Spawn Area")]
        [SerializeField] private bool useRandomPositionInArea = false;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(5f, 0f, 5f);
        
        // Wave settings
        private int currentWave;
        private int enemiesToSpawn;
        private float healthMultiplier = 1f;
        private float damageMultiplier = 1f;
        
        // Spawning state
        private bool isSpawningEnabled = false;
        private int enemiesSpawned = 0;
        private Coroutine spawnCoroutine;
        private bool isDoneSpawning = false;
        
        // Reference to the network manager
        private NetworkManager networkManager;
        
        // Reference to object pool
        private NetworkObjectPool objectPool;
        
        private void Start()
        {
            // Get network manager
            networkManager = NetworkManager.Singleton;
            
            // Get object pool
            objectPool = NetworkObjectPool.Instance;
            if (objectPool == null)
            {
                Debug.LogError("[EnemySpawner] NetworkObjectPool instance not found!");
            }
            
            // Validate spawn points
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                // If no spawn points assigned, use this transform
                spawnPoints = new Transform[] { transform };
            }
        }
        
        /// <summary>
        /// Called when the wave state is entered
        /// </summary>
        public void OnWaveStateEntered()
        {
            // Only start spawning if we're the server
            if (networkManager != null && networkManager.IsServer)
            {
                isSpawningEnabled = true;
                isDoneSpawning = false;
                
                Debug.Log($"[EnemySpawner] Wave state entered, starting to spawn enemies for wave {currentWave}");
                
                // Start spawning coroutine
                if (spawnCoroutine != null)
                {
                    StopCoroutine(spawnCoroutine);
                }
                
                spawnCoroutine = StartCoroutine(SpawnEnemies());
            }
        }
        
        /// <summary>
        /// Called when the wave state is exited
        /// </summary>
        public void OnWaveStateExited()
        {
            isSpawningEnabled = false;
            
            // Stop spawning coroutine
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            
            Debug.Log("[EnemySpawner] Wave state exited, stopped spawning enemies");
        }
        
        /// <summary>
        /// Configure the spawner for a specific wave
        /// </summary>
        public void ConfigureForWave(int waveNumber, int enemyCount, float health, float damage)
        {
            currentWave = waveNumber;
            enemiesToSpawn = enemyCount;
            healthMultiplier = health;
            damageMultiplier = damage;
            
            // Reset spawning state
            enemiesSpawned = 0;
            isDoneSpawning = false;
            
            Debug.Log($"[EnemySpawner] Configured for wave {waveNumber}: {enemyCount} enemies, {health}x health, {damage}x damage");
        }
        
        /// <summary>
        /// Enable or disable spawning
        /// </summary>
        public void SetSpawningEnabled(bool enabled)
        {
            isSpawningEnabled = enabled;
            
            if (!enabled && spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            else if (enabled && spawnCoroutine == null && !isDoneSpawning && networkManager != null && networkManager.IsServer)
            {
                spawnCoroutine = StartCoroutine(SpawnEnemies());
            }
        }
        
        /// <summary>
        /// Check if spawner has finished spawning all enemies
        /// </summary>
        public bool IsDoneSpawning()
        {
            return isDoneSpawning;
        }
        
        /// <summary>
        /// Coroutine to spawn enemies over time
        /// </summary>
        private IEnumerator SpawnEnemies()
        {
            // Initial delay
            yield return new WaitForSeconds(initialDelay);
            
            while (isSpawningEnabled && enemiesSpawned < enemiesToSpawn)
            {
                SpawnEnemy();
                enemiesSpawned++;
                
                yield return new WaitForSeconds(spawnInterval);
            }
            
            // Mark as done spawning
            isDoneSpawning = true;
            Debug.Log($"[EnemySpawner] Done spawning enemies for wave {currentWave}");
        }
        
        /// <summary>
        /// Spawn a single enemy
        /// </summary>
        private void SpawnEnemy()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogError("[EnemySpawner] No enemy prefabs assigned!");
                return;
            }
            
            if (objectPool == null)
            {
                Debug.LogError("[EnemySpawner] NetworkObjectPool instance not found!");
                return;
            }
            
            // Select random enemy prefab
            int prefabIndex = Random.Range(0, enemyPrefabs.Length);
            NetworkObject enemyPrefab = enemyPrefabs[prefabIndex];
            
            // Select random spawn point
            int spawnIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[spawnIndex];
            
            // Determine spawn position
            Vector3 spawnPosition;
            if (useRandomPositionInArea)
            {
                // Random position within area
                Vector3 randomOffset = new Vector3(
                    Random.Range(-spawnAreaSize.x/2, spawnAreaSize.x/2),
                    Random.Range(-spawnAreaSize.y/2, spawnAreaSize.y/2),
                    Random.Range(-spawnAreaSize.z/2, spawnAreaSize.z/2)
                );
                
                spawnPosition = spawnPoint.position + randomOffset;
            }
            else
            {
                // Use exact spawn point
                spawnPosition = spawnPoint.position;
            }
            
            // Get enemy from pool
            NetworkObject enemyObj = objectPool.Get(enemyPrefab);
            if (enemyObj != null)
            {
                // Position the enemy
                enemyObj.transform.position = spawnPosition;
                enemyObj.transform.rotation = spawnPoint.rotation;
                
                // Spawn on the network
                if (!enemyObj.IsSpawned)
                {
                    enemyObj.Spawn();
                }
                
                // Set up enemy attributes
                SetupEnemyAttributes(enemyObj.gameObject);
            }
            else
            {
                Debug.LogError($"[EnemySpawner] Failed to get enemy from pool: {enemyPrefab.name}");
            }
        }
        
        /// <summary>
        /// Set up enemy attributes based on wave settings
        /// </summary>
        private void SetupEnemyAttributes(GameObject enemyObj)
        {
            // Health component
            HealthComponent healthComponent = enemyObj.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.SetHealthMultiplier(healthMultiplier);
            }
            
            // Damage component
            EnemyDamage damageComponent = enemyObj.GetComponent<EnemyDamage>();
            if (damageComponent != null)
            {
                damageComponent.SetDamageMultiplier(damageMultiplier);
            }
            
            // AI component (pass the wave number)
            EnemyAI aiComponent = enemyObj.GetComponent<EnemyAI>();
            if (aiComponent != null)
            {
                aiComponent.InitializeForWave(currentWave);
            }
            
            // Entity component (if using the new architecture)
            EnemyEntity entityComponent = enemyObj.GetComponent<EnemyEntity>();
            if (entityComponent != null)
            {
                entityComponent.InitializeForWave(currentWave, healthMultiplier, damageMultiplier);
            }
        }
        
        /// <summary>
        /// Draw the spawn area in the editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (useRandomPositionInArea)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.25f); // Transparent red
                
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    foreach (Transform point in spawnPoints)
                    {
                        if (point != null)
                        {
                            Gizmos.DrawCube(point.position, spawnAreaSize);
                        }
                    }
                }
                else
                {
                    // Draw around this transform
                    Gizmos.DrawCube(transform.position, spawnAreaSize);
                }
            }
        }
    }
}
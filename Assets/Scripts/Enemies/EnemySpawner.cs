// Updated EnemySpawner.cs
using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemies = 10;
    
    private float _nextSpawnTime;
    private int _enemiesSpawned;
    private bool _spawningEnabled = false;
    
    // Network check flag
    private bool _networkReady = false;
    
    private void Awake()
    {
        // Initialize spawn time
        _nextSpawnTime = Time.time + spawnInterval;
        _enemiesSpawned = 0;
    }
    
    private void Start()
    {
        // Subscribe to network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
        else
        {
            // Try again later
            StartCoroutine(WaitForNetworkManager());
        }
    }
    
    private IEnumerator WaitForNetworkManager()
    {
        // Wait until NetworkManager is available or timeout after 10 seconds
        float timeoutTime = Time.time + 10f;
        
        while (NetworkManager.Singleton == null && Time.time < timeoutTime)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            Debug.Log("EnemySpawner found NetworkManager after waiting");
        }
        else
        {
            Debug.LogError("NetworkManager not available after timeout!");
        }
    }
    
    private void OnServerStarted()
    {
        Debug.Log("EnemySpawner detected server started - network is ready");
        _networkReady = true;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
    
    private void Update()
    {
        // Only spawn if enabled, network is ready, and we're the server
        if (!_spawningEnabled || !_networkReady || 
            NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;
        
        // Check if it's time to spawn a new enemy
        if (Time.time >= _nextSpawnTime && _enemiesSpawned < maxEnemies)
        {
            SpawnEnemy();
            _nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    public void SetSpawningEnabled(bool enabled)
    {
        _spawningEnabled = enabled;
        Debug.Log($"Enemy spawning {(_spawningEnabled ? "enabled" : "disabled")}");
    }
    
    private void SpawnEnemy()
    {
        // Double check we're the server and network is ready
        if (!NetworkManager.Singleton.IsServer || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Tried to spawn enemy but network is not ready!");
            return;
        }
        
        // Spawn the enemy
        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * 2f;
        spawnPosition.y = 0.5f; // Keep on ground plane
        
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
        
        if (networkObject != null)
        {
            // Safe spawn
            try
            {
                networkObject.Spawn();
                _enemiesSpawned++;
                Debug.Log($"Enemy spawned ({_enemiesSpawned}/{maxEnemies})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to spawn enemy: {e.Message}");
                Destroy(enemy);
            }
        }
        else
        {
            Debug.LogError("Enemy prefab missing NetworkObject component!");
            Destroy(enemy);
        }
    }
    
    // Called when game state changes to wave state
    public void OnWaveStateEntered()
    {
        SetSpawningEnabled(true);
    }
    
    // Called when game state changes away from wave state
    public void OnWaveStateExited()
    {
        SetSpawningEnabled(false);
    }
}
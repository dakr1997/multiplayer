using UnityEngine;
using Unity.Netcode;

public class EnemySpawner : NetworkBehaviour
{
    // This script is attached to an empty GameObject in the scene
    // It spawns creatures at regular intervals
    // It spawns them at the spawnPoint's position and rotation
    // Assign the enemy prefab and spawn point in the Inspector

    public GameObject creaturePrefab; // Assign this in the Inspector
    public Transform spawnPoint;   // Assign this in the Inspector
    public float initialSpawnInterval = 2f;
    [HideInInspector] public float spawnInterval;

    private float lastSpawnTime;

    void Start()
    {
        spawnInterval = initialSpawnInterval;
        lastSpawnTime = Time.time;
    }

    void Update()
    {
        // Only the server should handle spawning
        if (!IsServer) return;

        // Spawn enemies at regular intervals
        if (Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnCreature();
            lastSpawnTime = Time.time;
        }
    }

    void SpawnCreature()
    {
        // Check if enemyPrefab and spawnPoint are assigned
        if (creaturePrefab == null || spawnPoint == null)
        {
            Debug.LogError("Assign creaturePrefab and spawnPoint in the Inspector!");
            return;
        }

        // Instantiate the enemy and spawn it on the network
        GameObject enemy = Instantiate(
            creaturePrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Make sure the enemy has a NetworkObject component for networking
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();

        // Spawn the enemy across all clients (only on the server)
        if (networkObject != null)
        {
            networkObject.Spawn(); // This will spawn the object across the network
        }

        Debug.Log("Enemy spawned at: " + spawnPoint.position);
    }
}
